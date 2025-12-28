using BLL.Interfaces;
using BLL.Interfaces.Infrastructure;
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

    public async Task<object> LoginAsync(string username, string password)
    {
        string cacheKey = $"token_{username}";
        
        if (_cache.TryGetValue(cacheKey, out string? cachedToken) && !string.IsNullOrEmpty(cachedToken))
        {
            try
            {
                return await _profileApi.GetProfileAsync(cachedToken);
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
        
        var token = await _loginApi.LoginAsync(username, password);
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1)
        };
        _cache.Set(cacheKey, token, cacheOptions);
        return await _profileApi.GetProfileAsync(token);
    }
}