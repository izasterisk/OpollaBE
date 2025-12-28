using BLL.Interfaces.Infrastructure;

namespace Infrastructure.Authentication;

public class ProfileApi : IProfileApi
{
    private readonly ApiHelper _apiHelper;
    private readonly string _profileUrl;

    public ProfileApi(ApiHelper apiHelper)
    {
        _apiHelper = apiHelper;
        var baseUrl = Environment.GetEnvironmentVariable("URL") 
                      ?? throw new InvalidOperationException("URL not found in environment variables");
        _profileUrl = $"{baseUrl.TrimEnd('/')}/user/profile";
    }
    
    public async Task<object> GetProfileAsync(string token)
    {
        return await _apiHelper.GetWithAuthAsync<object>(_profileUrl, token) 
            ?? throw new Exception("Get profile failed: Invalid response from server");
    }
}