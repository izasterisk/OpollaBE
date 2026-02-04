using System.ComponentModel.DataAnnotations;

namespace BLL.DTOs.GoogleSheets;

public class GoogleSheetsSyncRequestDTO
{
    public string? Token { get; set; } = string.Empty;
}

public class GoogleSheetsSyncResponseDTO
{
    public string Message { get; set; } = string.Empty;
}
