namespace BLL.DTOs.Classes;

public class ClassPagingResponseDTO
{
    public List<ClassItemDTO> Data { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public int TotalPages { get; set; }
}

public class ClassItemDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? ClosedDate { get; set; }
    public bool IsActive { get; set; }
    public int TotalStudent { get; set; }
    public int HubId { get; set; }
    public int ClassStatusId { get; set; }
    public string AvatarType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public ClassReportDTO? Report { get; set; }
    public HomeLearningReportDTO? HomeLearningReport { get; set; }
    public int? TeacherId { get; set; }
    public int CenterId { get; set; }
    public int CurriculumId { get; set; }
    public int CSOId { get; set; }
}

public class ClassReportDTO
{
    public int Id { get; set; }
    public double Attendance { get; set; }
    public double? WorkbookCompletion { get; set; }
    public double? WorkbookScore { get; set; }
    public int APO { get; set; }
    public double? LearningObjective { get; set; }
    public int TotalSession { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int ClassId { get; set; }
}

public class HomeLearningReportDTO
{
    public int Id { get; set; }
    public double AppCompletion { get; set; }
    public double AppScore { get; set; }
    public long AppTime { get; set; }
    public int TotalHomeLearning { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int ClassId { get; set; }
}
