using BLL.DTOs.GoogleSheets;
using BLL.DTOs.Students;
using BLL.Helper;
using BLL.Interfaces;
using BLL.Interfaces.Infrastructure;

namespace BLL.Services;

public class GoogleSheetsSyncService : IGoogleSheetsSyncService
{
    private readonly IClassService _classService;
    private readonly IStudentService _studentService;
    private readonly IGoogleSheetsApi _googleSheetsApi;
    private readonly ILoginService _loginService;
    private readonly string email;
    private readonly string password;
    private readonly string editSheetId;
    private readonly string readSheetId;

    public GoogleSheetsSyncService(IClassService classService, IStudentService studentService,
        IGoogleSheetsApi googleSheetsApi, ILoginService loginService)
    {
        _classService = classService;
        _studentService = studentService;
        _googleSheetsApi = googleSheetsApi;
        _loginService = loginService;
        
        email = Environment.GetEnvironmentVariable("EMAIL")
                        ?? throw new InvalidOperationException("EMAIL not found in environment variables");
        password = Environment.GetEnvironmentVariable("PASSWORD")
                    ?? throw new InvalidOperationException("PASSWORD not found in environment variables");
        editSheetId = Environment.GetEnvironmentVariable("GOOGLE_SHEET_EDIT")
                            ?? throw new InvalidOperationException("GOOGLE_SHEET_EDIT not found in environment variables");
        readSheetId = Environment.GetEnvironmentVariable("GOOGLE_SHEET_READ")
                      ?? throw new InvalidOperationException("GOOGLE_SHEET_READ not found in environment variables");
    }
    
    public async Task<Dictionary<string, string>> GetClassesWithEcAsync(CancellationToken ct = default)
    {
        const string range = "Details Schedule!C1:Q500"; 

        var rawData = await _googleSheetsApi.ReadDataAsync(readSheetId, range, ct);
        var classesDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

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
                    classesDictionary[classValue] = rawEcValue;
                }
            }
        }
        return classesDictionary;
    }

    public async Task<GoogleSheetsSyncResponseDTO> SyncStudentDataToSheetAsync(
        GoogleSheetsSyncRequestDTO request,
        CancellationToken cancellationToken = default)
    {
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

        // 1. Get all classes (use large pageSize to get all)
        var classesResult = await _classService.GetAllClassesAsync(token, page: 1, pageSize: 1000, cancellationToken);
        var allClasses = classesResult.Data;

        // 2. Build data for sheet
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
            var classAppCompletionStr = FormatPercentage(classAppCompletion);
            var workbookCompletion = classItem.Report?.WorkbookCompletion;
            var workbookCompletionStr = FormatPercentage(workbookCompletion);

            foreach (var student in students)
            {
                var studentAppCompletion = student.HomeLearningReport?.AppCompletion;
                var studentAppCompletionStr = FormatPercentage(studentAppCompletion);

                sheetData.Add(new List<object>
                {
                    classItem.Name,           // Column A: Class name
                    classAppCompletionStr,    // Column B: Class App Completion
                    student.Name,             // Column C: Student name
                    studentAppCompletionStr,  // Column D: Student App Completion
                    workbookCompletionStr     // Column E: Workbook Completion
                });

                currentRow++;
            }

            var classEndRow = currentRow - 1;
            classMergeRanges.Add((classStartRow, classEndRow));
        }

        // 3. Sync to Google Sheets
        var updatedAt = DateHelper.GetVietnamNow();
        await _googleSheetsApi.SyncStudentDataAsync(
            editSheetId,
            sheetName,
            sheetData,
            classMergeRanges,
            updatedAt,
            cancellationToken);

        return new GoogleSheetsSyncResponseDTO
        {
            Message = $"Successfully synced."
        };
    }

    private static string FormatPercentage(double? value)
    {
        if (value == null)
            return "0%";
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:F2}%", value); // Format: 81.00%
    }
}
