namespace BLL.DTOs.Authentication;

public class ProfileResponseDTO
{
    public string Token { get; set; } = string.Empty;
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? LastLogin { get; set; }
    public DateTime? LockedAt { get; set; }
    public string? Phone { get; set; }
    public int HubId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public StaffDTO? Staff { get; set; }
    public List<object> Parents { get; set; } = new();
    public object? Teacher { get; set; }
}

public class StaffDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsSuperAdmin { get; set; }
    public int HubId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int UserId { get; set; }
}
