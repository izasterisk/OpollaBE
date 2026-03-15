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

    public GoogleSheetsSyncController(IGoogleSheetsSyncService syncService)
    {
        _syncService = syncService;
    }

    [HttpHead("check")]
    public IActionResult Check() => Ok();

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

            await _syncService.SyncStudentDataToSheetAsync(request, cancellationToken);

            return SuccessResponse("Cập nhật thành công");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }
}
