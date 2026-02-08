using Google.Apis.Sheets.v4.Data;

namespace Infrastructure.GoogleSheet;

/// <summary>
/// Column configuration for Google Sheets sync
/// </summary>
public static class SheetColumns
{
    // Data columns (0-indexed)
    public const int ClassName = 0;           // Column A
    public const int ClassAppCompletion = 1;  // Column B
    public const int StudentName = 2;         // Column C
    public const int StudentAppCompletion = 3;// Column D
    public const int WorkbookCompletion = 4;  // Column E
    public const int ECName = 5;              // Column F
    public const int TotalColumns = 6;
    
    // Summary/metadata columns (0-indexed)
    public const int TimestampColumn = 8;     // Column I (for timestamp)
    public const int AvgAppColumn = 7;        // Column H (for average app completion)
    public const int AvgWbColumn = 8;         // Column I (for average workbook completion)
    
    // EC Summary columns (0-indexed)
    public const int EcNameColumn = 7;        // Column H (EC name)
    public const int EcYesterdayColumn = 8;   // Column I (yesterday app avg)
    public const int EcTodayColumn = 9;       // Column J (today app avg)
    public const int EcAvgWbColumn = 10;      // Column K (average workbook)
    public const int EcSummaryTotalColumns = 11; // Total columns including EC summary
    
    // Summary/metadata rows (0-indexed)
    public const int TimestampStartRow = 1;   // Row 2 (for date)
    public const int AverageRow = 5;          // Row 6 (for averages)
    public const int EcDateRow = 7;           // Row 8 (for date headers I8, J8)
    public const int EcDataStartRow = 8;      // Row 9 (EC data starts here)
    
    // Columns that need merge (class-level data)
    public static readonly int[] MergeColumns = { ClassName, ClassAppCompletion, WorkbookCompletion, ECName };
    
    // Columns that show percentage and need color coding
    public static readonly int[] PercentageColumns = { ClassAppCompletion, StudentAppCompletion, WorkbookCompletion };
    
    // Columns that should be bold
    public static readonly int[] BoldColumns = { ClassName, ECName };
    
    // Column widths in pixels
    public static readonly int[] ColumnWidths = { 157, 157, 300, 157, 157, 130 };
    
    // Row height in pixels
    public const int RowHeight = 34;
}

public static class GoogleSheetsApiHelper
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
    
        // Tạo màu nghiêng về xanh lá: green cao hơn, red và blue thấp hơn
        float r = 0.65f + (float)random.NextDouble() * 0.25f;  // 0.65 -> 0.90
        float g = 0.75f + (float)random.NextDouble() * 0.25f;  // 0.75 -> 1.00 (cao nhất)
        float b = 0.65f + (float)random.NextDouble() * 0.25f;  // 0.65 -> 0.90

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
    
    /// <summary>
    /// Convert 0-indexed column index to Excel column letter (e.g., 0 -> A, 25 -> Z, 26 -> AA)
    /// </summary>
    public static string GetColumnLetter(int columnIndex)
    {
        var letter = "";
        while (columnIndex >= 0)
        {
            letter = (char)('A' + (columnIndex % 26)) + letter;
            columnIndex = columnIndex / 26 - 1;
        }
        return letter;
    }
    
    /// <summary>
    /// Build EC summary rows: [ECName, YesterdayApp, TodayApp, AvgWb]
    /// Uses the dataset with more records as primary key source
    /// </summary>
    public static List<List<string>> BuildEcSummaryRows(
        Dictionary<string, string>? todayData,
        Dictionary<string, string>? yesterdayData,
        Dictionary<string, string> avgEcWb)
    {
        // Collect all EC names from all sources
        var allEcNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        if (todayData != null) foreach (var key in todayData.Keys) allEcNames.Add(key);
        if (yesterdayData != null) foreach (var key in yesterdayData.Keys) allEcNames.Add(key);
        
        // Use the dataset with more records as primary, fallback to the other
        var primaryKeys = (todayData?.Count ?? 0) >= (yesterdayData?.Count ?? 0) 
            ? todayData?.Keys 
            : yesterdayData?.Keys;
        
        // Build ordered list: primary keys first, then remaining
        var orderedEcNames = new List<string>();
        if (primaryKeys != null)
        {
            orderedEcNames.AddRange(primaryKeys);
        }
        foreach (var name in allEcNames)
        {
            if (!orderedEcNames.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                orderedEcNames.Add(name);
            }
        }

        var rows = new List<List<string>>();
        foreach (var ecName in orderedEcNames)
        {
            var yesterdayValue = yesterdayData != null && yesterdayData.TryGetValue(ecName, out var yVal) ? yVal : "-";
            var todayValue = todayData != null && todayData.TryGetValue(ecName, out var tVal) ? tVal : "-";
            var wbValue = avgEcWb.TryGetValue(ecName, out var wVal) ? wVal : "-";
            
            rows.Add(new List<string> { ecName, yesterdayValue, todayValue, wbValue });
        }
        
        return rows;
    }
}
