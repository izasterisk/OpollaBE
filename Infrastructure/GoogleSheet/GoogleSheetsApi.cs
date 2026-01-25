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
    private static readonly Color RedColor = new() { Red = 0.878f, Green = 0.4f, Blue = 0.4f }; // #e06666
    private static readonly Color YellowColor = new() { Red = 1f, Green = 0.898f, Blue = 0.6f }; // #ffe599
    private static readonly Color GreenColor = new() { Red = 0.576f, Green = 0.769f, Blue = 0.49f }; // #93c47d
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
        DateTime updatedAt,
        CancellationToken cancellationToken = default)
    {
        // Get sheet ID from sheet name
        var spreadsheet = await _sheetsService.Spreadsheets.Get(spreadsheetId).ExecuteAsync(cancellationToken);
        var sheet = spreadsheet.Sheets.FirstOrDefault(s => s.Properties.Title == sheetName)
            ?? throw new InvalidOperationException($"Sheet '{sheetName}' not found");
        var sheetId = sheet.Properties.SheetId;

        // Build batch update request
        var requests = new List<Request>();

        // 1. Clear all data from A2:E (keep header row 1)
        requests.Add(new Request
        {
            UpdateCells = new UpdateCellsRequest
            {
                Range = new GridRange
                {
                    SheetId = sheetId,
                    StartRowIndex = 1, // Row 2 (0-indexed)
                    StartColumnIndex = 0, // Column A
                    EndColumnIndex = 5 // Column E (exclusive)
                },
                Fields = "userEnteredValue,userEnteredFormat"
            }
        });

        // 2. Unmerge all cells in columns A, B, and E (from row 2 onwards)
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

        requests.Add(new Request
        {
            UnmergeCells = new UnmergeCellsRequest
            {
                Range = new GridRange
                {
                    SheetId = sheetId,
                    StartRowIndex = 1,
                    StartColumnIndex = 4,
                    EndColumnIndex = 5 // Column E
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
            
            // Set column widths (A=157, B=157, C=300, D=157, E=157)
            var columnWidths = new[] { 157, 157, 300, 157, 157 };
            for (var col = 0; col < 5; col++)
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
                            HorizontalAlignment = "CENTER",
                            TextFormat = new TextFormat
                            {
                                FontFamily = "Calibri",
                                FontSize = 12,
                                Bold = j == 0 // Bold for column A (Class name)
                            }
                        }
                    };

                    // Apply color based on App Completion (columns B, D, and E)
                    if (j == 1 || j == 3 || j == 4) // Column B (Class App Completion), D (Student App Completion), or E (Workbook Completion)
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
                        EndColumnIndex = 5
                    },
                    Rows = rows,
                    Fields = "userEnteredValue,userEnteredFormat"
                }
            });

            // Execute format and data update
            var batchFormatRequest = new BatchUpdateSpreadsheetRequest { Requests = formatRequests };
            await _sheetsService.Spreadsheets.BatchUpdate(batchFormatRequest, spreadsheetId)
                .ExecuteAsync(cancellationToken);

            // Update "Last updated at" timestamp (H2: date, H3: time)
            var dateStr = updatedAt.ToString("dd/MM/yyyy");
            var timeStr = updatedAt.ToString("HH:mm:ss");

            var timestampRange = $"{sheetName}!H2:H3";
            var timestampValues = new ValueRange
            {
                Values = new List<IList<object>>
                {
                    new List<object> { dateStr },
                    new List<object> { timeStr }
                }
            };

            var timestampRequest = _sheetsService.Spreadsheets.Values.Update(timestampValues, spreadsheetId, timestampRange);
            timestampRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            await timestampRequest.ExecuteAsync(cancellationToken);
        }

        // 4. Merge cells for class columns (A, B, and E)
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

                    // Merge column E (Workbook Completion)
                    mergeRequests.Add(new Request
                    {
                        MergeCells = new MergeCellsRequest
                        {
                            Range = new GridRange
                            {
                                SheetId = sheetId,
                                StartRowIndex = startRow,
                                EndRowIndex = endRow + 1,
                                StartColumnIndex = 4, // Column E
                                EndColumnIndex = 5
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
