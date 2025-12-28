using BLL.Interfaces.Infrastructure;
using BLL.DTOs.Authentication;

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
    
    public async Task<ProfileResponseDTO> GetProfileAsync(string token)
    {
        return await _apiHelper.GetWithAuthAsync<ProfileResponseDTO>(_profileUrl, token) 
            ?? throw new Exception("Get profile failed: Invalid response from server");
    }
}