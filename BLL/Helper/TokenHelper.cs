using Microsoft.Extensions.Caching.Memory;

namespace BLL.Helper;

public class TokenHelper
{
    private readonly IMemoryCache _cache;
    
    public TokenHelper(IMemoryCache cache)
    {
        _cache = cache;
    }
    
    /// <summary>
    /// Get cached token for making authenticated requests
    /// </summary>
    /// <param name="username">Username to retrieve token for</param>
    /// <returns>Cached token</returns>
    /// <exception cref="InvalidOperationException">Thrown when token not found in cache</exception>
    public string GetToken(string username)
    {
        string cacheKey = $"token_{username}";
        
        if (_cache.TryGetValue(cacheKey, out string? cachedToken) && !string.IsNullOrEmpty(cachedToken))
        {
            return cachedToken;
        }
        
        throw new InvalidOperationException($"Token not found in cache for user: {username}. Please login first.");
    }
}