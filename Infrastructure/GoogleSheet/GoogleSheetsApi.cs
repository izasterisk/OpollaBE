using BLL.Interfaces.Infrastructure;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace Infrastructure.GoogleSheet;

public class GoogleSheetsApi : IGoogleSheetsApi
{
    private readonly SheetsService _sheetsService;

    public GoogleSheetsApi()
    {
        var base64Key = Environment.GetEnvironmentVariable("GOOGLE_SHEET_API_KEY_BASE64")
            ?? throw new InvalidOperationException("GOOGLE_SHEET_API_KEY_BASE64 not found in environment variables");
        
        var jsonKey = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64Key));

        #pragma warning disable CS0618 // Type or member is obsolete
        var credential = GoogleCredential.FromJson(jsonKey)
            .CreateScoped(SheetsService.Scope.Spreadsheets);
        #pragma warning restore CS0618

        _sheetsService = new SheetsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "OpollaBE"
        });
    }

    public async Task SyncStudentDataAsync(
        string spreadsheetId,
        string sheetName,
        IList<IList<object>> data,
        List<(int startRow, int endRow)> classMergeRanges,
        CancellationToken cancellationToken = default)
    {
        // Get sheet ID from sheet name
        var spreadsheet = await _sheetsService.Spreadsheets.Get(spreadsheetId).ExecuteAsync(cancellationToken);
        var sheet = spreadsheet.Sheets.FirstOrDefault(s => s.Properties.Title == sheetName)
            ?? throw new InvalidOperationException($"Sheet '{sheetName}' not found");
        var sheetId = sheet.Properties.SheetId;

        // Build batch update request
        var requests = new List<Request>();

        // 1. Clear all data from A2:D (keep header row 1)
        requests.Add(new Request
        {
            UpdateCells = new UpdateCellsRequest
            {
                Range = new GridRange
                {
                    SheetId = sheetId,
                    StartRowIndex = 1, // Row 2 (0-indexed)
                    StartColumnIndex = 0, // Column A
                    EndColumnIndex = 4 // Column D (exclusive)
                },
                Fields = "userEnteredValue"
            }
        });

        // 2. Unmerge all cells in columns A and B (from row 2 onwards)
        requests.Add(new Request
        {
            UnmergeCells = new UnmergeCellsRequest
            {
                Range = new GridRange
                {
                    SheetId = sheetId,
                    StartRowIndex = 1,
                    StartColumnIndex = 0,
                    EndColumnIndex = 2 // Columns A and B
                }
            }
        });

        // Execute clear and unmerge first
        if (requests.Count > 0)
        {
            var batchClearRequest = new BatchUpdateSpreadsheetRequest { Requests = requests };
            await _sheetsService.Spreadsheets.BatchUpdate(batchClearRequest, spreadsheetId)
                .ExecuteAsync(cancellationToken);
        }

        // 3. Write new data starting from A2
        if (data.Count > 0)
        {
            var range = $"{sheetName}!A2:D{data.Count + 1}";
            var valueRange = new ValueRange
            {
                Values = data
            };

            var updateRequest = _sheetsService.Spreadsheets.Values.Update(valueRange, spreadsheetId, range);
            updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            await updateRequest.ExecuteAsync(cancellationToken);
        }

        // 4. Merge cells for class columns (A and B)
        if (classMergeRanges.Count > 0)
        {
            var mergeRequests = new List<Request>();

            foreach (var (startRow, endRow) in classMergeRanges)
            {
                // Only merge if there's more than 1 row
                if (endRow > startRow)
                {
                    // Merge column A (Class name)
                    mergeRequests.Add(new Request
                    {
                        MergeCells = new MergeCellsRequest
                        {
                            Range = new GridRange
                            {
                                SheetId = sheetId,
                                StartRowIndex = startRow, // 0-indexed, row 2 = index 1
                                EndRowIndex = endRow + 1, // exclusive
                                StartColumnIndex = 0, // Column A
                                EndColumnIndex = 1
                            },
                            MergeType = "MERGE_ALL"
                        }
                    });

                    // Merge column B (Class App Completion)
                    mergeRequests.Add(new Request
                    {
                        MergeCells = new MergeCellsRequest
                        {
                            Range = new GridRange
                            {
                                SheetId = sheetId,
                                StartRowIndex = startRow,
                                EndRowIndex = endRow + 1,
                                StartColumnIndex = 1, // Column B
                                EndColumnIndex = 2
                            },
                            MergeType = "MERGE_ALL"
                        }
                    });
                }
            }

            if (mergeRequests.Count > 0)
            {
                var batchMergeRequest = new BatchUpdateSpreadsheetRequest { Requests = mergeRequests };
                await _sheetsService.Spreadsheets.BatchUpdate(batchMergeRequest, spreadsheetId)
                    .ExecuteAsync(cancellationToken);
            }
        }
    }
}
