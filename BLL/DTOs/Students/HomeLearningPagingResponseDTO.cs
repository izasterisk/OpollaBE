namespace BLL.DTOs.Students;

public class HomeLearningPagingResponseDTO
{
    public List<HomeLearningDTO> Data { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public int TotalPages { get; set; }
}

public class HomeLearningDTO
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty; // Endpoint POST /api/Student this field will be null
    public int ClassId { get; set; }
    public DateTime AssignDate { get; set; }
    public int CmsResourceId { get; set; }
    public int CmsHomeLearningId { get; set; }
    public double? AppCompletion { get; set; }
    public double? AppScore { get; set; }
    public int? AppTime { get; set; }
    public string Skill { get; set; } = string.Empty;
    public DateTime? CompletedAt { get; set; }
    public string? LastSession { get; set; }
    public string? UnitName { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int StudentClassId { get; set; }
}
