namespace BLL.Helper;

public class GoogleSheetHelper
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
}