namespace BLL.Interfaces.Infrastructure;

public interface IGoogleSheetsApi
{
    Task<IList<IList<object>>> ReadDataAsync(
        string spreadsheetId,
        string range,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear all data and merge from specified range, then write new data with merged cells
    /// </summary>
    /// <param name="spreadsheetId">Google Spreadsheet ID</param>
    /// <param name="sheetName">Sheet name (tab name)</param>
    /// <param name="data">Data to write - each inner list is a row [ClassName, ClassAppCompletion, StudentName, StudentAppCompletion]</param>
    /// <param name="classMergeRanges">List of (startRow, endRow) for merging class columns A and B</param>
    /// <param name="updatedAt">Timestamp to display in the sheet (Vietnam timezone)</param>
    /// <param name="avgApp">Average App Completion to display in H6</param>
    /// <param name="avgWb">Average Workbook Completion to display in I6</param>
    /// <param name="avgTodayEcApp">Dictionary of EC name to App completion percentage for today (cached data)</param>
    /// <param name="avgYesterdayEcApp">Dictionary of EC name to App completion percentage for yesterday (cached data)</param>
    /// <param name="avgEcWb">Dictionary of EC name to Workbook completion percentage</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SyncStudentDataAsync(
        string spreadsheetId,
        string sheetName,
        IList<IList<object>> data,
        List<(int startRow, int endRow)> classMergeRanges,
        DateTime updatedAt,
        string avgApp, string avgWb,
        Dictionary<string, string>? avgTodayEcApp, Dictionary<string, string>? avgYesterdayEcApp,
        Dictionary<string, string> avgEcWb,
        CancellationToken cancellationToken = default);
}
