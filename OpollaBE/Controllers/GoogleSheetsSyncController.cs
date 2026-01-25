using Microsoft.AspNetCore.Mvc;
using BLL.Interfaces;
using BLL.DTOs;
using BLL.DTOs.GoogleSheets;

namespace OpollaBE.Controllers;

[Route("api/[controller]")]
[ApiController]
public class GoogleSheetsSyncController : BaseController
{
    private readonly IGoogleSheetsSyncService _syncService;
    private readonly ILogger<GoogleSheetsSyncController> _logger;

    public GoogleSheetsSyncController(
        IGoogleSheetsSyncService syncService,
        ILogger<GoogleSheetsSyncController> logger)
    {
        _syncService = syncService;
        _logger = logger;
    }

    /// <summary>
    /// Sync student data (Class, ClassAppCompletion, StudentName, StudentAppCompletion) to Google Sheets
    /// </summary>
    /// <param name="request">Request containing token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success message</returns>
    [HttpPost]
    public async Task<ActionResult<APIResponse>> SyncToGoogleSheets(
        [FromBody] GoogleSheetsSyncRequestDTO request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var validationResult = ValidateModel();
            if (validationResult != null) return validationResult;

            _logger.LogInformation("Starting Google Sheets sync...");
            
            await _syncService.SyncStudentDataToSheetAsync(request, cancellationToken);
            
            _logger.LogInformation("Google Sheets sync completed");

            return SuccessResponse("Cập nhật thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SyncToGoogleSheets: {Message}", ex.Message);
            return HandleException(ex);
        }
    }
}
