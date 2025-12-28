using Microsoft.AspNetCore.Mvc;
using BLL.Interfaces;
using BLL.DTOs;
using BLL.DTOs.Authentication;

namespace OpollaBE.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LoginController : BaseController
{
    private readonly ILoginService _loginService;

    public LoginController(ILoginService loginService)
    {
        _loginService = loginService;
    }

    [HttpPost]
    public async Task<ActionResult<APIResponse>> Login([FromBody] LoginRequestDTO request)
    {
        try
        {
            var validationResult = ValidateModel();
            if (validationResult != null) return validationResult;

            var profile = await _loginService.LoginAsync(request.Username, request.Password);
            return SuccessResponse(profile);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }
}