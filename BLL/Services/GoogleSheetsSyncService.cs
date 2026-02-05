using BLL.DTOs.GoogleSheets;
using BLL.DTOs.Students;
using BLL.Helper;
using BLL.Interfaces;
using BLL.Interfaces.Infrastructure;
using Microsoft.Extensions.Caching.Memory;

namespace BLL.Services;

public class GoogleSheetsSyncService : IGoogleSheetsSyncService
{
    private readonly IClassService _classService;
    private readonly IStudentService _studentService;
    private readonly IGoogleSheetsApi _googleSheetsApi;
    private readonly ILoginService _loginService;
    private readonly IMemoryCache _cache;
    private readonly string email;
    private readonly string password;
    private readonly string editSheetId;
    private readonly string readSheetId;

    public GoogleSheetsSyncService(IClassService classService, IStudentService studentService,
        IGoogleSheetsApi googleSheetsApi, ILoginService loginService, IMemoryCache cache)
    {
        _classService = classService;
        _studentService = studentService;
        _googleSheetsApi = googleSheetsApi;
        _loginService = loginService;
        _cache = cache;
        
        email = Environment.GetEnvironmentVariable("EMAIL")
                        ?? throw new InvalidOperationException("EMAIL not found in environment variables");
        password = Environment.GetEnvironmentVariable("PASSWORD")
                    ?? throw new InvalidOperationException("PASSWORD not found in environment variables");
        editSheetId = Environment.GetEnvironmentVariable("GOOGLE_SHEET_EDIT")
                            ?? throw new InvalidOperationException("GOOGLE_SHEET_EDIT not found in environment variables");
        readSheetId = Environment.GetEnvironmentVariable("GOOGLE_SHEET_READ")
                      ?? throw new InvalidOperationException("GOOGLE_SHEET_READ not found in environment variables");
    }
    
    private async Task<(Dictionary<string, string>, Dictionary<string, string>)> GetClassesWithEcAsync(CancellationToken ct = default)
    {
        const string range = "Details Schedule!C1:Q500"; 

        var rawData = await _googleSheetsApi.ReadDataAsync(readSheetId, range, ct);
        var classesDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var teachersDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < rawData.Count; i++)
        {
            var row = rawData[i];
        
            if (row.Count > 0)
            {
                // Cột C là index 0 trong vùng chọn
                var classValue = row[0]?.ToString()?.Trim() ?? string.Empty;

                if (classValue.Contains("VGP", StringComparison.OrdinalIgnoreCase))
                {
                    // Cột Q cách cột C là 14 vị trí (C=0, D=1... Q=14)
                    // Cần check Count > 14 vì Google bỏ qua các ô trống ở cuối hàng
                    var rawEcValue = row.Count > 14 ? row[14]?.ToString()?.Trim() ?? "UNDEFINED" : "UNDEFINED";
                    var rawTeacherValue = row.Count > 13 ? row[13]?.ToString()?.Trim() ?? "UNDEFINED" : "UNDEFINED";
                    classesDictionary[classValue] = rawEcValue;
                    teachersDictionary[classValue] = rawTeacherValue;
                }
            }
        }
        return (classesDictionary, teachersDictionary);
    }
    
    private void CaculateEachEcClasses(Dictionary<string, (double TotalValue, int Count)> apps)
    {
        var today = DateHelper.GetVietnamDate();
        var avgEcAppDate = new Dictionary<string, string>();
        
        foreach (var app in apps)
        {
            var avg = GoogleSheetsHelper.FormatPercentage(app.Value.TotalValue / app.Value.Count);
            avgEcAppDate[app.Key] = avg;
        }
        _cache.Set($"{today:dd-MM}", avgEcAppDate, TimeSpan.FromDays(2));
    }
    
    public async Task<GoogleSheetsSyncResponseDTO> SyncStudentDataToSheetAsync(
        GoogleSheetsSyncRequestDTO request,
        CancellationToken cancellationToken = default)
    {
        var avgEcApp = new Dictionary<string, (double TotalValue, int Count)>();
        var avgEcWbs = new Dictionary<string, (double TotalValue, int Count)>();
        string token;
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            var res = await _loginService.LoginAsync(email, password, cancellationToken);
            token = res.Token;
        }
        else
        {
            token = request.Token;
        }
        
        const string sheetName = "Low ATLS Completion";
        double totalApp = 0; double totalWb = 0; var appCounted = 0; var wbCounted = 0;

        // 1. Get EC data from Google Sheets
        var classes = await GetClassesWithEcAsync(cancellationToken);
        var classesWithEc = classes.Item1;

        // 2. Get all classes (use large pageSize to get all)
        var classesResult = await _classService.GetAllClassesAsync(token, page: 1, pageSize: 1000, cancellationToken);
        var allClasses = classesResult.Data;

        // 3. Build data for sheet
        var sheetData = new List<IList<object>>();
        var classMergeRanges = new List<(int startRow, int endRow)>();
        var currentRow = 1; // Start from row 2 (index 1, since row 1 is header)

        foreach (var classItem in allClasses)
        {
            // Get students for this class (use large pageSize to get all)
            var studentsResult = await _studentService.GetAllStudentsAsync(
                new StudentRequestDTO { Token = token, ClassId = classItem.Id.ToString() },
                page: 1,
                pageSize: 1000,
                cancellationToken);
            
            var students = studentsResult.Data;
            
            if (students.Count == 0)
                continue;

            var classStartRow = currentRow;
            var classAppCompletion = classItem.HomeLearningReport?.AppCompletion;
            var classAppCompletionStr = GoogleSheetsHelper.FormatPercentage(classAppCompletion);
            var workbookCompletion = classItem.Report?.WorkbookCompletion;
            var workbookCompletionStr = GoogleSheetsHelper.FormatPercentage(workbookCompletion);
            
            var ecName = classesWithEc.TryGetValue(classItem.Name, out var ec) ? ec : "UNDEFINED";
            if (classAppCompletion != null && ecName != "UNDEFINED")
            {
                if (avgEcApp.TryGetValue(GoogleSheetsHelper.GetFirstWord(ecName), out var total))
                {
                    avgEcApp[ecName] = (total.TotalValue + classAppCompletion.Value, total.Count + 1);
                }
                else
                {
                    avgEcApp[ecName] = (classAppCompletion.Value, 1);
                }
            }
            if (workbookCompletion != null && ecName != "UNDEFINED")
            {
                if (avgEcWbs.TryGetValue(GoogleSheetsHelper.GetFirstWord(ecName), out var total))
                {
                    avgEcWbs[ecName] = (total.TotalValue + workbookCompletion.Value, total.Count + 1);
                }
                else
                {
                    avgEcWbs[ecName] = (workbookCompletion.Value, 1);
                }
            }
            
            foreach (var student in students)
            {
                var studentAppCompletion = student.HomeLearningReport?.AppCompletion;
                var studentAppCompletionStr = GoogleSheetsHelper.FormatPercentage(studentAppCompletion);
                
                totalApp += studentAppCompletion ?? 0;
                appCounted++;

                sheetData.Add(new List<object>
                {
                    classItem.Name,           // Column A: Class name
                    classAppCompletionStr,    // Column B: Class App Completion
                    student.Name,             // Column C: Student name
                    studentAppCompletionStr,  // Column D: Student App Completion
                    workbookCompletionStr,    // Column E: Workbook Completion
                    ecName                    // Column F: EC Name
                });
                currentRow++;
            }

            var classEndRow = currentRow - 1;
            classMergeRanges.Add((classStartRow, classEndRow));

            totalWb += workbookCompletion ?? 0;
            wbCounted++;
        }
        
        var avgWb = GoogleSheetsHelper.FormatPercentage(totalWb/wbCounted);
        var avgApp = GoogleSheetsHelper.FormatPercentage(totalApp/appCounted);

        // 4. Calculate and save EC averages
        CaculateEachEcClasses(avgEcApp);
        var avgEcWb = new Dictionary<string, string>();
        foreach (var i in avgEcWbs)
        {
            avgEcWb.Add(i.Key, GoogleSheetsHelper.FormatPercentage(i.Value.TotalValue / i.Value.Count));
        }
        
        // 5. Get cached data for today and yesterday
        var today = DateHelper.GetVietnamDate();
        var yesterday = today.AddDays(-1);
        var todayData = GoogleSheetsHelper.GetCachedEcData(_cache, $"{today:dd-MM}");
        var yesterdayData = GoogleSheetsHelper.GetCachedEcData(_cache, $"{yesterday:dd-MM}");

        // 6. Sync to Google Sheets
        var updatedAt = DateHelper.GetVietnamNow();
        await _googleSheetsApi.SyncStudentDataAsync(editSheetId, sheetName,
            sheetData, classMergeRanges, updatedAt, avgApp, avgWb, todayData, yesterdayData, 
            avgEcWb, cancellationToken);

        return new GoogleSheetsSyncResponseDTO
        {
            Message = "Successfully synced."
        };
    }
}
