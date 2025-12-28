using Microsoft.AspNetCore.Mvc;
using BLL.Interfaces;
using BLL.DTOs;
using BLL.DTOs.Classes;

namespace OpollaBE.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ClassController : BaseController
{
    private readonly IClassService _classService;

    public ClassController(IClassService classService)
    {
        _classService = classService;
    }

    [HttpPost]
    public async Task<ActionResult<APIResponse>> GetClasses(
        [FromBody] ClassRequestDTO request,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var classes = await _classService.GetAllClassesAsync(request.Token, page, pageSize, cancellationToken);
            return SuccessResponse(classes);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }
}
