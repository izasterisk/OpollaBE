using Microsoft.Extensions.Caching.Memory;

namespace BLL.Helper;

public static class GoogleSheetsHelper
{
    public static string GetFirstWord(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;
        
        int spaceIndex = input.IndexOf(' ');
        
        if (spaceIndex == -1)
            return input; // Chỉ có 1 từ
        
        return input.Substring(0, spaceIndex); // Lấy từ đầu tiên
    }
    
    public static string FormatPercentage(double? value)
    {
        if (value == null)
            return "0%";
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:F2}%", value); // Format: 81.00%
    }
    
    public static Dictionary<string, string>? GetCachedEcData(IMemoryCache cache, string cacheKey)
    {
        return cache.TryGetValue(cacheKey, out Dictionary<string, string>? data) ? data : null;
    }
}