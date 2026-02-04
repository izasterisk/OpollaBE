using BLL.Interfaces.Infrastructure;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace Infrastructure.GoogleSheet;

public class GoogleSheetsApi : IGoogleSheetsApi
{
    private readonly SheetsService _sheetsService;

    public GoogleSheetsApi()
    {
        var clientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID")
            ?? throw new InvalidOperationException("GOOGLE_CLIENT_ID not found in environment variables");
        var clientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET")
            ?? throw new InvalidOperationException("GOOGLE_CLIENT_SECRET not found in environment variables");
        var refreshToken = Environment.GetEnvironmentVariable("GOOGLE_REFRESH_TOKEN")
            ?? throw new InvalidOperationException("GOOGLE_REFRESH_TOKEN not found in environment variables");

        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets { ClientId = clientId, ClientSecret = clientSecret }
        });

        var tokenResponse = new TokenResponse { RefreshToken = refreshToken };
        var credential = new UserCredential(flow, "user", tokenResponse);

        _sheetsService = new SheetsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "OpollaBE"
        });
    }
    
    public async Task<IList<IList<object>>> ReadDataAsync(
        string spreadsheetId, 
        string range, CancellationToken cancellationToken = default)
    {
        var request = _sheetsService.Spreadsheets.Values.Get(spreadsheetId, range);
        var response = await request.ExecuteAsync(cancellationToken);
        return response.Values ?? new List<IList<object>>();
    }

    public async Task SyncStudentDataAsync(
        string spreadsheetId,
        string sheetName,
        IList<IList<object>> data,
        List<(int startRow, int endRow)> classMergeRanges,
        DateTime updatedAt,
        CancellationToken cancellationToken = default)
    {
        var sheetId = await GetSheetIdAsync(spreadsheetId, sheetName, cancellationToken);

        // Step 1: Clear data and unmerge cells
        await ClearAndUnmergeAsync(spreadsheetId, sheetId, cancellationToken);

        // Step 2: Write data with formatting
        if (data.Count > 0)
        {
            await WriteDataWithFormattingAsync(spreadsheetId, sheetId, data, cancellationToken);
            await UpdateTimestampAsync(spreadsheetId, sheetName, updatedAt, cancellationToken);
        }

        // Step 3: Merge cells and add borders
        if (classMergeRanges.Count > 0)
        {
            await MergeCellsAsync(spreadsheetId, sheetId, classMergeRanges, cancellationToken);
            await AddClassBordersAsync(spreadsheetId, sheetId, classMergeRanges, cancellationToken);
        }
    }

    #region Private Helper Methods

    private async Task<int?> GetSheetIdAsync(string spreadsheetId, string sheetName, CancellationToken ct)
    {
        var spreadsheet = await _sheetsService.Spreadsheets.Get(spreadsheetId).ExecuteAsync(ct);
        var sheet = spreadsheet.Sheets.FirstOrDefault(s => s.Properties.Title == sheetName)
            ?? throw new InvalidOperationException($"Sheet '{sheetName}' not found");
        return sheet.Properties.SheetId;
    }

    private async Task ClearAndUnmergeAsync(string spreadsheetId, int? sheetId, CancellationToken ct)
    {
        var requests = new List<Request>
        {
            // Clear all data from row 2 onwards
            CreateClearRequest(sheetId),
            // Unmerge columns A and B
            CreateUnmergeRequest(sheetId, SheetColumns.ClassName, SheetColumns.ClassAppCompletion + 1),
            // Unmerge columns E and F
            CreateUnmergeRequest(sheetId, SheetColumns.WorkbookCompletion, SheetColumns.TotalColumns)
        };

        await ExecuteBatchUpdateAsync(spreadsheetId, requests, ct);
    }

    private async Task WriteDataWithFormattingAsync(string spreadsheetId, int? sheetId, IList<IList<object>> data, CancellationToken ct)
    {
        var requests = new List<Request>();
        
        // Add column width requests
        requests.AddRange(CreateColumnWidthRequests(sheetId));
        
        // Add row height request
        requests.Add(CreateRowHeightRequest(sheetId, data.Count));
        
        // Add cells update request
        var rows = BuildRowsWithFormatting(data);
        requests.Add(CreateUpdateCellsRequest(sheetId, rows, data.Count));

        await ExecuteBatchUpdateAsync(spreadsheetId, requests, ct);
    }

    private async Task UpdateTimestampAsync(string spreadsheetId, string sheetName, DateTime updatedAt, CancellationToken ct)
    {
        var dateStr = updatedAt.ToString("dd/MM/yyyy");
        var timeStr = updatedAt.ToString("HH:mm:ss");

        var timestampRange = $"{sheetName}!I2:I3";
        var timestampValues = new ValueRange
        {
            Values = new List<IList<object>>
            {
                new List<object> { dateStr },
                new List<object> { timeStr }
            }
        };

        var request = _sheetsService.Spreadsheets.Values.Update(timestampValues, spreadsheetId, timestampRange);
        request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
        await request.ExecuteAsync(ct);
    }

    private async Task MergeCellsAsync(string spreadsheetId, int? sheetId, List<(int startRow, int endRow)> mergeRanges, CancellationToken ct)
    {
        var requests = new List<Request>();

        foreach (var (startRow, endRow) in mergeRanges)
        {
            if (endRow <= startRow) continue;

            foreach (var colIndex in SheetColumns.MergeColumns)
            {
                requests.Add(CreateMergeRequest(sheetId, startRow, endRow, colIndex));
            }
        }

        if (requests.Count > 0)
        {
            await ExecuteBatchUpdateAsync(spreadsheetId, requests, ct);
        }
    }

    private async Task AddClassBordersAsync(string spreadsheetId, int? sheetId, List<(int startRow, int endRow)> mergeRanges, CancellationToken ct)
    {
        var requests = mergeRanges
            .Select(range => CreateBorderRequest(sheetId, range.startRow, range.endRow))
            .ToList();

        if (requests.Count > 0)
        {
            await ExecuteBatchUpdateAsync(spreadsheetId, requests, ct);
        }
    }

    private async Task ExecuteBatchUpdateAsync(string spreadsheetId, List<Request> requests, CancellationToken ct)
    {
        var batchRequest = new BatchUpdateSpreadsheetRequest { Requests = requests };
        await _sheetsService.Spreadsheets.BatchUpdate(batchRequest, spreadsheetId).ExecuteAsync(ct);
    }

    #endregion

    #region Request Builders

    private static Request CreateClearRequest(int? sheetId)
    {
        return new Request
        {
            UpdateCells = new UpdateCellsRequest
            {
                Range = new GridRange
                {
                    SheetId = sheetId,
                    StartRowIndex = 1,
                    StartColumnIndex = 0,
                    EndColumnIndex = SheetColumns.TotalColumns
                },
                Fields = "userEnteredValue,userEnteredFormat"
            }
        };
    }

    private static Request CreateUnmergeRequest(int? sheetId, int startCol, int endCol)
    {
        return new Request
        {
            UnmergeCells = new UnmergeCellsRequest
            {
                Range = new GridRange
                {
                    SheetId = sheetId,
                    StartRowIndex = 1,
                    StartColumnIndex = startCol,
                    EndColumnIndex = endCol
                }
            }
        };
    }

    private static IEnumerable<Request> CreateColumnWidthRequests(int? sheetId)
    {
        for (var col = 0; col < SheetColumns.TotalColumns; col++)
        {
            yield return new Request
            {
                UpdateDimensionProperties = new UpdateDimensionPropertiesRequest
                {
                    Range = new DimensionRange
                    {
                        SheetId = sheetId,
                        Dimension = "COLUMNS",
                        StartIndex = col,
                        EndIndex = col + 1
                    },
                    Properties = new DimensionProperties { PixelSize = SheetColumns.ColumnWidths[col] },
                    Fields = "pixelSize"
                }
            };
        }
    }

    private static Request CreateRowHeightRequest(int? sheetId, int dataCount)
    {
        return new Request
        {
            UpdateDimensionProperties = new UpdateDimensionPropertiesRequest
            {
                Range = new DimensionRange
                {
                    SheetId = sheetId,
                    Dimension = "ROWS",
                    StartIndex = 1,
                    EndIndex = dataCount + 1
                },
                Properties = new DimensionProperties { PixelSize = SheetColumns.RowHeight },
                Fields = "pixelSize"
            }
        };
    }

    private static Request CreateUpdateCellsRequest(int? sheetId, List<RowData> rows, int dataCount)
    {
        return new Request
        {
            UpdateCells = new UpdateCellsRequest
            {
                Range = new GridRange
                {
                    SheetId = sheetId,
                    StartRowIndex = 1,
                    StartColumnIndex = 0,
                    EndRowIndex = dataCount + 1,
                    EndColumnIndex = SheetColumns.TotalColumns
                },
                Rows = rows,
                Fields = "userEnteredValue,userEnteredFormat"
            }
        };
    }

    private static Request CreateMergeRequest(int? sheetId, int startRow, int endRow, int colIndex)
    {
        return new Request
        {
            MergeCells = new MergeCellsRequest
            {
                Range = new GridRange
                {
                    SheetId = sheetId,
                    StartRowIndex = startRow,
                    EndRowIndex = endRow + 1,
                    StartColumnIndex = colIndex,
                    EndColumnIndex = colIndex + 1
                },
                MergeType = "MERGE_ALL"
            }
        };
    }

    private static Request CreateBorderRequest(int? sheetId, int startRow, int endRow)
    {
        return new Request
        {
            UpdateBorders = new UpdateBordersRequest
            {
                Range = new GridRange
                {
                    SheetId = sheetId,
                    StartRowIndex = startRow,
                    EndRowIndex = endRow + 1,
                    StartColumnIndex = 0,
                    EndColumnIndex = SheetColumns.TotalColumns
                },
                Top = new Border { Style = "SOLID_THICK", Color = GoogleSheetsHelper.BlackColor },
                Bottom = new Border { Style = "SOLID_THICK", Color = GoogleSheetsHelper.BlackColor },
                Left = new Border { Style = "SOLID_THICK", Color = GoogleSheetsHelper.BlackColor },
                Right = new Border { Style = "SOLID_THICK", Color = GoogleSheetsHelper.BlackColor }
            }
        };
    }

    #endregion

    #region Cell Formatting

    private static List<RowData> BuildRowsWithFormatting(IList<IList<object>> data)
    {
        var rows = new List<RowData>();
        
        foreach (var rowData in data)
        {
            var cells = new List<CellData>();
            
            for (var colIndex = 0; colIndex < rowData.Count; colIndex++)
            {
                var cellValue = rowData[colIndex]?.ToString() ?? "";
                cells.Add(CreateCellData(cellValue, colIndex));
            }
            
            rows.Add(new RowData { Values = cells });
        }
        
        return rows;
    }

    private static CellData CreateCellData(string value, int colIndex)
    {
        var cellData = new CellData
        {
            UserEnteredValue = new ExtendedValue { StringValue = value },
            UserEnteredFormat = new CellFormat
            {
                Borders = CreateDefaultBorders(),
                VerticalAlignment = "MIDDLE",
                HorizontalAlignment = "CENTER",
                TextFormat = new TextFormat
                {
                    FontFamily = "Calibri",
                    FontSize = 12,
                    Bold = SheetColumns.BoldColumns.Contains(colIndex)
                }
            }
        };

        // Apply background color based on column type
        ApplyCellBackgroundColor(cellData, value, colIndex);

        return cellData;
    }

    private static Borders CreateDefaultBorders()
    {
        return new Borders
        {
            Top = new Border { Style = "SOLID", Color = GoogleSheetsHelper.BlackColor },
            Bottom = new Border { Style = "SOLID", Color = GoogleSheetsHelper.BlackColor },
            Left = new Border { Style = "SOLID", Color = GoogleSheetsHelper.BlackColor },
            Right = new Border { Style = "SOLID", Color = GoogleSheetsHelper.BlackColor }
        };
    }

    private static void ApplyCellBackgroundColor(CellData cellData, string value, int colIndex)
    {
        if (SheetColumns.PercentageColumns.Contains(colIndex))
        {
            cellData.UserEnteredFormat.BackgroundColor = GoogleSheetsHelper.GetColorForPercentage(value);
        }
        else if (colIndex == SheetColumns.ECName)
        {
            cellData.UserEnteredFormat.BackgroundColor = GoogleSheetsHelper.GetColorForEC(value);
        }
    }

    #endregion
}
