namespace BLL.Interfaces.Infrastructure;

public interface IGoogleSheetsApi
{
    /// <summary>
    /// Clear all data and merge from specified range, then write new data with merged cells
    /// </summary>
    /// <param name="spreadsheetId">Google Spreadsheet ID</param>
    /// <param name="sheetName">Sheet name (tab name)</param>
    /// <param name="data">Data to write - each inner list is a row [ClassName, ClassAppCompletion, StudentName, StudentAppCompletion]</param>
    /// <param name="classMergeRanges">List of (startRow, endRow) for merging class columns A and B</param>
    /// <param name="updatedAt">Timestamp to display in the sheet (Vietnam timezone)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SyncStudentDataAsync(
        string spreadsheetId,
        string sheetName,
        IList<IList<object>> data,
        List<(int startRow, int endRow)> classMergeRanges,
        DateTime updatedAt,
        CancellationToken cancellationToken = default);
}
