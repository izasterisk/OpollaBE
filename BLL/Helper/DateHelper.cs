namespace BLL.Helper;

public class DateHelper
{
    /// <summary>
    /// Tính số ngày kể từ ngày được chỉ định đến hôm nay (bao gồm cả ngày bắt đầu).
    /// </summary>
    /// <param name="startDate">Ngày bắt đầu</param>
    /// <returns>Số ngày đã trôi qua (inclusive)</returns>
    /// <example>
    /// GetDaysSince(new DateOnly(2025, 12, 27)) khi hôm nay là 2025-12-29 → trả về 3
    /// </example>
    public static int GetDaysSince(DateOnly startDate)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var daysDifference = today.DayNumber - startDate.DayNumber;
        return daysDifference + 1;
    }

    /// <summary>
    /// Tính toán trang chứa bản ghi có AssignDate = choosenDate.
    /// Giả định: Dữ liệu được sắp xếp theo ngày giảm dần (mới nhất → cũ nhất)
    /// và mỗi ngày có đúng 1 bản ghi.
    /// </summary>
    /// <param name="choosenDate">Ngày cần tìm</param>
    /// <param name="pageSize">Số bản ghi mỗi trang</param>
    /// <param name="firstAssignDate">AssignDate của bản ghi đầu tiên (mới nhất)</param>
    /// <param name="lastAssignDate">AssignDate của bản ghi cuối cùng (cũ nhất)</param>
    /// <returns>Số trang chứa bản ghi (bắt đầu từ 1), hoặc 1 nếu không tìm thấy</returns>
    /// <example>
    /// firstAssignDate = 2025-12-30 (trang 1, record 1)
    /// choosenDate = 2025-12-28 (trang 1, record 3 nếu pageSize >= 3)
    /// pageSize = 2
    /// → Kết quả: trang 2 (vì record thứ 3 nằm ở trang 2)
    /// </example>
    public static int CalculatePageForDate(DateOnly choosenDate, int pageSize, 
        DateOnly firstAssignDate, DateOnly lastAssignDate)
    {
        // Kiểm tra choosenDate có nằm trong range không
        if (choosenDate < lastAssignDate || choosenDate > firstAssignDate)
            return 1; // Trả về trang 1 mặc định nếu ngoài range
        
        // Tính số ngày từ firstAssignDate (mới nhất) đến choosenDate
        // firstAssignDate là record đầu tiên (index 0)
        var daysFromFirst = firstAssignDate.DayNumber - choosenDate.DayNumber;
        
        // Record index (0-based): index = số ngày cách xa firstAssignDate
        var recordIndex = daysFromFirst;
        
        // Page number (1-based): page = (recordIndex / pageSize) + 1
        var pageNumber = (recordIndex / pageSize) + 1;
        
        return pageNumber;
    }

    /// <summary>
    /// Lấy thời gian hiện tại theo múi giờ Việt Nam (UTC+7)
    /// </summary>
    /// <returns>DateTime theo múi giờ Việt Nam</returns>
    public static DateTime GetVietnamTimeNow()
    {
        TimeZoneInfo vietnamTimeZone;
        try
        {
            vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);
    }
}