using BLL.Interfaces.Infrastructure;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace Infrastructure.GoogleSheet;

public class GoogleSheetsApi : IGoogleSheetsApi
{
    private readonly SheetsService _sheetsService;

    // Colors for conditional formatting
    private static readonly Color RedColor = new() { Red = 0.92f, Green = 0.35f, Blue = 0.35f }; // Darker red
    private static readonly Color YellowColor = new() { Red = 1f, Green = 0.85f, Blue = 0.2f }; // Darker yellow
    private static readonly Color GreenColor = new() { Red = 0.3f, Green = 0.8f, Blue = 0.3f }; // Darker green
    private static readonly Color BlackColor = new() { Red = 0f, Green = 0f, Blue = 0f }; // Black for borders

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
                Fields = "userEnteredValue,userEnteredFormat"
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

        // 3. Write new data with formatting starting from A2
        if (data.Count > 0)
        {
            var formatRequests = new List<Request>();
            
            // Set column widths (A=157, B=157, C=300, D=157)
            var columnWidths = new[] { 157, 157, 300, 157 };
            for (var col = 0; col < 4; col++)
            {
                formatRequests.Add(new Request
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
                        Properties = new DimensionProperties { PixelSize = columnWidths[col] },
                        Fields = "pixelSize"
                    }
                });
            }

            // Set row heights (34 pixels for data rows)
            formatRequests.Add(new Request
            {
                UpdateDimensionProperties = new UpdateDimensionPropertiesRequest
                {
                    Range = new DimensionRange
                    {
                        SheetId = sheetId,
                        Dimension = "ROWS",
                        StartIndex = 1, // Start from row 2
                        EndIndex = data.Count + 1
                    },
                    Properties = new DimensionProperties { PixelSize = 34 },
                    Fields = "pixelSize"
                }
            });

            // Build cells with formatting
            var rows = new List<RowData>();
            for (var i = 0; i < data.Count; i++)
            {
                var rowData = data[i];
                var cells = new List<CellData>();

                for (var j = 0; j < rowData.Count; j++)
                {
                    var cellValue = rowData[j]?.ToString() ?? "";
                    var cellData = new CellData
                    {
                        UserEnteredValue = new ExtendedValue { StringValue = cellValue },
                        UserEnteredFormat = new CellFormat
                        {
                            Borders = new Borders
                            {
                                Top = new Border { Style = "SOLID", Color = BlackColor },
                                Bottom = new Border { Style = "SOLID", Color = BlackColor },
                                Left = new Border { Style = "SOLID", Color = BlackColor },
                                Right = new Border { Style = "SOLID", Color = BlackColor }
                            },
                            VerticalAlignment = "MIDDLE",
                            HorizontalAlignment = "CENTER"
                        }
                    };

                    // Apply color based on App Completion (columns B and D)
                    if (j == 1 || j == 3) // Column B (Class App Completion) or D (Student App Completion)
                    {
                        var color = GetColorForPercentage(cellValue);
                        cellData.UserEnteredFormat.BackgroundColor = color;
                    }

                    cells.Add(cellData);
                }

                rows.Add(new RowData { Values = cells });
            }

            // Update cells with values and formatting
            formatRequests.Add(new Request
            {
                UpdateCells = new UpdateCellsRequest
                {
                    Range = new GridRange
                    {
                        SheetId = sheetId,
                        StartRowIndex = 1,
                        StartColumnIndex = 0,
                        EndRowIndex = data.Count + 1,
                        EndColumnIndex = 4
                    },
                    Rows = rows,
                    Fields = "userEnteredValue,userEnteredFormat"
                }
            });

            // Execute format and data update
            var batchFormatRequest = new BatchUpdateSpreadsheetRequest { Requests = formatRequests };
            await _sheetsService.Spreadsheets.BatchUpdate(batchFormatRequest, spreadsheetId)
                .ExecuteAsync(cancellationToken);
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

    private static Color GetColorForPercentage(string percentageStr)
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
}
