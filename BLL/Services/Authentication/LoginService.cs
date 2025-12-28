using BLL.Interfaces;
using BLL.Interfaces.Infrastructure;
using BLL.DTOs.Authentication;
using Microsoft.Extensions.Caching.Memory;

namespace BLL.Services.Authentication;

public class LoginService : ILoginService
{
    private readonly ILoginApi  _loginApi;
    private readonly IProfileApi _profileApi;
    private readonly IMemoryCache _cache;
    
    public LoginService(ILoginApi loginApi, IProfileApi profileApi, IMemoryCache cache)
    {
        _loginApi = loginApi;
        _profileApi = profileApi;
        _cache = cache;
    }

    public async Task<ProfileResponseDTO> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        string cacheKey = $"token_{username}";
        
        if (_cache.TryGetValue(cacheKey, out string? cachedToken) && !string.IsNullOrEmpty(cachedToken))
        {
            try
            {
                var cachedProfile = await _profileApi.GetProfileAsync(cachedToken, cancellationToken);
                cachedProfile.Token = cachedToken;
                return cachedProfile;
            }
            catch (HttpRequestException)
            {
                _cache.Remove(cacheKey);
            }
            catch (Exception ex) when (ex.Message.Contains("401") || ex.Message.Contains("403") || ex.Message.Contains("Unauthorized"))
            {
                _cache.Remove(cacheKey);
            }
        }
        
        var token = await _loginApi.LoginAsync(username, password, cancellationToken);
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
        };
        _cache.Set(cacheKey, token, cacheOptions);
        
        var profile = await _profileApi.GetProfileAsync(token, cancellationToken);
        profile.Token = token;
        return profile;
    }

    public Task LogoutAsync(string username)
    {
        var cacheKey = $"token_{username}";
        _cache.Remove(cacheKey);
        return Task.CompletedTask;
    }
}