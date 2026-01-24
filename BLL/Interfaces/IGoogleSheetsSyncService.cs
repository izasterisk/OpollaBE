using BLL.DTOs.GoogleSheets;

namespace BLL.Interfaces;

public interface IGoogleSheetsSyncService
{
    Task<GoogleSheetsSyncResponseDTO> SyncStudentDataToSheetAsync(
        GoogleSheetsSyncRequestDTO request,
        CancellationToken cancellationToken = default);
}
