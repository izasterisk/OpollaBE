using BLL.Interfaces;
using BLL.Interfaces.Infrastructure;

namespace BLL.Services.Authentication;

public class LoginService : ILoginService
{
    private readonly ILoginApi  _loginApi;

    public LoginService(ILoginApi loginApi)
    {
        _loginApi = loginApi;
    }

    public async Task<object> LoginAsync(string username, string password)
    {
        return await _loginApi.LoginAsync(username, password);
    }
}