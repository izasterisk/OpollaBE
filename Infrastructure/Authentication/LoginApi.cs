using System.Text.Json;
using BLL.Interfaces.Infrastructure;

namespace Infrastructure.Authentication;

public class LoginApi : ILoginApi
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _loginUrl;

    public LoginApi(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        _loginUrl = Environment.GetEnvironmentVariable("LOGIN_URL") 
            ?? throw new InvalidOperationException("LOGIN_URL not found in environment variables");
    }

    public async Task<object> LoginAsync(string username, string password)
    {
        var formData = new Dictionary<string, string>
        {
            { "email", username },
            { "password", password }
        };

        using var httpClient = _httpClientFactory.CreateClient();
        using var content = new FormUrlEncodedContent(formData);
        var response = await httpClient.PostAsync(_loginUrl, content);
        response.EnsureSuccessStatusCode();

        var jsonString = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(jsonString);
        
        // Return the entire JSON response as object
        return JsonSerializer.Deserialize<object>(jsonString) 
            ?? throw new Exception("Login failed: Invalid response from server");
    }
}