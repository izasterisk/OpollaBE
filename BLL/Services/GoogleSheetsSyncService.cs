using BLL.DTOs.GoogleSheets;
using BLL.DTOs.Students;
using BLL.Interfaces;
using BLL.Interfaces.Infrastructure;

namespace BLL.Services;

public class GoogleSheetsSyncService : IGoogleSheetsSyncService
{
    private readonly IClassService _classService;
    private readonly IStudentService _studentService;
    private readonly IGoogleSheetsApi _googleSheetsApi;

    public GoogleSheetsSyncService(
        IClassService classService,
        IStudentService studentService,
        IGoogleSheetsApi googleSheetsApi)
    {
        _classService = classService;
        _studentService = studentService;
        _googleSheetsApi = googleSheetsApi;
    }

    public async Task<GoogleSheetsSyncResponseDTO> SyncStudentDataToSheetAsync(
        GoogleSheetsSyncRequestDTO request,
        CancellationToken cancellationToken = default)
    {
        // Get spreadsheet config from environment
        var spreadsheetId = Environment.GetEnvironmentVariable("GOOGLE_SHEET")
            ?? throw new InvalidOperationException("GOOGLE_SHEET not found in environment variables");
        
        const string sheetName = "Low ATLS Completion";

        // 1. Get all classes (use large pageSize to get all)
        var classesResult = await _classService.GetAllClassesAsync(request.Token, page: 1, pageSize: 1000, cancellationToken);
        var allClasses = classesResult.Data;

        // 2. Build data for sheet
        var sheetData = new List<IList<object>>();
        var classMergeRanges = new List<(int startRow, int endRow)>();
        var classSummaries = new List<ClassSyncSummaryDTO>();
        var totalStudents = 0;
        var currentRow = 1; // Start from row 2 (index 1, since row 1 is header)

        foreach (var classItem in allClasses)
        {
            // Get students for this class (use large pageSize to get all)
            var studentsResult = await _studentService.GetAllStudentsAsync(
                new StudentRequestDTO { Token = request.Token, ClassId = classItem.Id.ToString() },
                page: 1,
                pageSize: 1000,
                cancellationToken);
            
            var students = studentsResult.Data;
            
            if (students.Count == 0)
                continue;

            var classStartRow = currentRow;
            var classAppCompletion = classItem.HomeLearningReport?.AppCompletion;
            var classAppCompletionStr = FormatPercentage(classAppCompletion);

            foreach (var student in students)
            {
                var studentAppCompletion = student.HomeLearningReport?.AppCompletion;
                var studentAppCompletionStr = FormatPercentage(studentAppCompletion);

                sheetData.Add(new List<object>
                {
                    classItem.Name,           // Column A: Class name
                    classAppCompletionStr,    // Column B: Class App Completion
                    student.Name,             // Column C: Student name
                    studentAppCompletionStr   // Column D: Student App Completion
                });

                currentRow++;
            }

            var classEndRow = currentRow - 1;
            
            // Add merge range for this class (only if more than 1 student)
            classMergeRanges.Add((classStartRow, classEndRow));

            // Add summary
            classSummaries.Add(new ClassSyncSummaryDTO
            {
                ClassId = classItem.Id,
                ClassName = classItem.Name,
                ClassAppCompletion = classAppCompletion,
                StudentCount = students.Count
            });

            totalStudents += students.Count;
        }

        // 3. Sync to Google Sheets
        await _googleSheetsApi.SyncStudentDataAsync(
            spreadsheetId,
            sheetName,
            sheetData,
            classMergeRanges,
            cancellationToken);

        return new GoogleSheetsSyncResponseDTO
        {
            TotalClasses = classSummaries.Count,
            TotalStudents = totalStudents,
            ClassSummaries = classSummaries,
            Message = $"Successfully synced {totalStudents} students from {classSummaries.Count} classes to Google Sheets"
        };
    }

    private static string FormatPercentage(double? value)
    {
        if (value == null)
            return "0%";
        
        return $"{value:F2}%"; // Format: 81.00%
    }
}
