namespace BLL.DTOs.Students;

public class StudentPagingResponseDTO
{
    public List<StudentDTO> Data { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public int TotalPages { get; set; }
}

public class StudentDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? NickName { get; set; }
    public DateTime? Dob { get; set; }
    public string Gender { get; set; } = string.Empty;
    public int HubId { get; set; }
    public string AvatarType { get; set; } = string.Empty;
    public string? UsingDevice { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public AvatarDTO? Avatar { get; set; }
    public int? AvatarId { get; set; }
    public int ParentId { get; set; }
    public List<BuiltAvatarDTO> BuiltAvatar { get; set; } = new();
    public StudentReportDTO? Report { get; set; }
    public HomeLearningReportDTO? HomeLearningReport { get; set; }
}

public class AvatarDTO
{
    public int Id { get; set; }
    public string Originalname { get; set; } = string.Empty;
    public string Mimetype { get; set; } = string.Empty;
    public int Size { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool IsAPOFeedbackIcon { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int? CreatorId { get; set; }
    public int? GalleryId { get; set; }
    public int? AvatarBaseId { get; set; }
}

public class BuiltAvatarDTO
{
    public int Id { get; set; }
    public int APO { get; set; }
    public bool IsUsing { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTime LastUsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public AvatarImageDTO? Image { get; set; }
    public object? Accessory { get; set; }
    public int StudentId { get; set; }
    public int ImageId { get; set; }
    public int? AccessoryId { get; set; }
    public int BaseId { get; set; }
    public DateTime NextUseAt { get; set; }
}

public class AvatarImageDTO
{
    public int Id { get; set; }
    public string Originalname { get; set; } = string.Empty;
    public string Mimetype { get; set; } = string.Empty;
    public int Size { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool IsAPOFeedbackIcon { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int CreatorId { get; set; }
    public int? GalleryId { get; set; }
    public int? AvatarBaseId { get; set; }
}

public class StudentReportDTO
{
    public double? Attendance { get; set; }
    public double? WorkbookCompletion { get; set; }
    public double? WorkbookScore { get; set; }
    public int APO { get; set; }
    public double? LearningObjective { get; set; }
    public int TotalSession { get; set; }
}

public class HomeLearningReportDTO
{
    public double? AppCompletion { get; set; }
    public double? AppScore { get; set; }
    public int? AppTime { get; set; }
    public int TotalHomeLearning { get; set; }
}