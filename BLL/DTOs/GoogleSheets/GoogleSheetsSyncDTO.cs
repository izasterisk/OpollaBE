using System.ComponentModel.DataAnnotations;

namespace BLL.DTOs.GoogleSheets;

public class GoogleSheetsSyncRequestDTO
{
    [Required(ErrorMessage = "Token is required")]
    public string Token { get; set; } = string.Empty;
}

public class GoogleSheetsSyncResponseDTO
{
    public int TotalClasses { get; set; }
    public int TotalStudents { get; set; }
    public List<ClassSyncSummaryDTO> ClassSummaries { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}

public class ClassSyncSummaryDTO
{
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public double? ClassAppCompletion { get; set; }
    public int StudentCount { get; set; }
}
