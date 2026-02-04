using Google.Apis.Sheets.v4.Data;

namespace Infrastructure.GoogleSheet;

public static class GoogleSheetsHelper
{
    // Colors for conditional formatting
    public static readonly Color RedColor = new() { Red = 0.878f, Green = 0.4f, Blue = 0.4f }; // #e06666
    public static readonly Color YellowColor = new() { Red = 1f, Green = 0.898f, Blue = 0.6f }; // #ffe599
    public static readonly Color GreenColor = new() { Red = 0.576f, Green = 0.769f, Blue = 0.49f }; // #93c47d
    public static readonly Color BlackColor = new() { Red = 0f, Green = 0f, Blue = 0f }; // Black for borders

    public static Color GetColorForPercentage(string percentageStr)
    {
        // Parse percentage value (e.g., "37,50%" or "0%")
        var cleanValue = percentageStr.Replace("%", "").Replace(",", ".").Trim();
        
        if (!double.TryParse(cleanValue, System.Globalization.NumberStyles.Any, 
            System.Globalization.CultureInfo.InvariantCulture, out var percentage))
        {
            return RedColor; // Default to red if parsing fails
        }

        // <= 50%: Red, > 50% and < 75%: Yellow, >= 75%: Green
        if (percentage <= 50)
            return RedColor;
        if (percentage < 75)
            return YellowColor;
        return GreenColor;
    }
    
    public static Color GetColorForEC(string rawEcName)
    {
        var ecName = GetFirstWord(rawEcName);
        
        // Nếu là UNDEFINED, trả về màu xám nhạt
        if (ecName == "UNDEFINED")
        {
            return new Color { Red = 0.85f, Green = 0.85f, Blue = 0.85f }; // #d9d9d9
        }
        
        // Tạo seed từ hash của tên EC để đảm bảo cùng EC có cùng màu
        var seed = ecName.GetHashCode();
        var random = new Random(seed);
    
        // Tạo giá trị từ 0.7 đến 1.0 để đảm bảo màu luôn nhạt/sáng
        float r = 0.7f + (float)random.NextDouble() * 0.3f;
        float g = 0.7f + (float)random.NextDouble() * 0.3f;
        float b = 0.7f + (float)random.NextDouble() * 0.3f;

        return new Color { Red = r, Green = g, Blue = b };
    }
    
    public static string GetFirstWord(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;
        
        int spaceIndex = input.IndexOf(' ');
        
        if (spaceIndex == -1)
            return input; // Chỉ có 1 từ
        
        return input.Substring(0, spaceIndex); // Lấy từ đầu tiên
    }
}
