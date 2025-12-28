using BLL.Interfaces;
using BLL.Interfaces.Infrastructure;

namespace BLL.Services.Authentication;

public class LoginService : ILoginService
{
    private readonly ILoginApi  _loginApi;
    private readonly IProfileApi _profileApi;
    
    public LoginService(ILoginApi loginApi, IProfileApi profileApi)
    {
        _loginApi = loginApi;
        _profileApi = profileApi;
    }

    public async Task<object> LoginAsync(string username, string password)
    {
        var token = await _loginApi.LoginAsync(username, password);
        return await _profileApi.GetProfileAsync(token);
    }
}