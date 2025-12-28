using System.Text.Json;
using BLL.Interfaces.Infrastructure;

namespace Infrastructure.Authentication;

public class LoginApi : ILoginApi
{
    private readonly ApiHelper _apiHelper;
    private readonly string _loginUrl;

    public LoginApi(ApiHelper apiHelper)
    {
        _apiHelper = apiHelper;
        var baseUrl = Environment.GetEnvironmentVariable("URL") 
            ?? throw new InvalidOperationException("URL not found in environment variables");
        _loginUrl = $"{baseUrl.TrimEnd('/')}/auth/login";
    }

    public async Task<string> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var formData = new Dictionary<string, string>
        {
            { "email", username },
            { "password", password }
        };

        var response = await _apiHelper.PostFormDataAsync<JsonElement>(_loginUrl, formData, cancellationToken);
        
        if (response.TryGetProperty("token", out var tokenElement))
        {
            return tokenElement.GetString() 
                ?? throw new Exception("Token is null in response");
        }
        
        throw new Exception("Login failed: Token not found in response");
    }
}