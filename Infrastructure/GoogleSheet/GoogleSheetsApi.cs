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
        // Get sheet ID from sheet name
        var spreadsheet = await _sheetsService.Spreadsheets.Get(spreadsheetId).ExecuteAsync(cancellationToken);
        var sheet = spreadsheet.Sheets.FirstOrDefault(s => s.Properties.Title == sheetName)
            ?? throw new InvalidOperationException($"Sheet '{sheetName}' not found");
        var sheetId = sheet.Properties.SheetId;

        // Build batch update request
        var requests = new List<Request>();

        // 1. Clear all data from A2:F (keep header row 1)
        requests.Add(new Request
        {
            UpdateCells = new UpdateCellsRequest
            {
                Range = new GridRange
                {
                    SheetId = sheetId,
                    StartRowIndex = 1, // Row 2 (0-indexed)
                    StartColumnIndex = 0, // Column A
                    EndColumnIndex = 6 // Column F (exclusive)
                },
                Fields = "userEnteredValue,userEnteredFormat"
            }
        });

        // 2. Unmerge all cells in columns A, B, E, and F (from row 2 onwards)
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
                    EndColumnIndex = 6 // Columns E and F
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
            
            // Set column widths (A=157, B=157, C=300, D=157, E=157, F=130)
            var columnWidths = new[] { 157, 157, 300, 157, 157, 130 };
            for (var col = 0; col < 6; col++)
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
                                Top = new Border { Style = "SOLID", Color = GoogleSheetsHelper.BlackColor },
                                Bottom = new Border { Style = "SOLID", Color = GoogleSheetsHelper.BlackColor },
                                Left = new Border { Style = "SOLID", Color = GoogleSheetsHelper.BlackColor },
                                Right = new Border { Style = "SOLID", Color = GoogleSheetsHelper.BlackColor }
                            },
                            VerticalAlignment = "MIDDLE",
                            HorizontalAlignment = "CENTER",
                            TextFormat = new TextFormat
                            {
                                FontFamily = "Calibri",
                                FontSize = 12,
                                Bold = j == 0 || j == 5 // Bold for column A (Class name) and F (EC Name)
                            }
                        }
                    };

                    // Apply color based on App Completion (columns B, D, and E)
                    if (j == 1 || j == 3 || j == 4) // Column B (Class App Completion), D (Student App Completion), or E (Workbook Completion)
                    {
                        var color = GoogleSheetsHelper.GetColorForPercentage(cellValue);
                        cellData.UserEnteredFormat.BackgroundColor = color;
                    }
                    
                    // Apply color for EC Name (column F)
                    if (j == 5) // Column F (EC Name)
                    {
                        var color = GoogleSheetsHelper.GetColorForEC(cellValue);
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
                        EndColumnIndex = 6
                    },
                    Rows = rows,
                    Fields = "userEnteredValue,userEnteredFormat"
                }
            });

            // Execute format and data update
            var batchFormatRequest = new BatchUpdateSpreadsheetRequest { Requests = formatRequests };
            await _sheetsService.Spreadsheets.BatchUpdate(batchFormatRequest, spreadsheetId)
                .ExecuteAsync(cancellationToken);

            // Update "Last updated at" timestamp (I2: date, I3: time)
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

            var timestampRequest = _sheetsService.Spreadsheets.Values.Update(timestampValues, spreadsheetId, timestampRange);
            timestampRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            await timestampRequest.ExecuteAsync(cancellationToken);
        }

        // 4. Merge cells for class columns (A, B, E, and F)
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

                    // Merge column F (EC Name)
                    mergeRequests.Add(new Request
                    {
                        MergeCells = new MergeCellsRequest
                        {
                            Range = new GridRange
                            {
                                SheetId = sheetId,
                                StartRowIndex = startRow,
                                EndRowIndex = endRow + 1,
                                StartColumnIndex = 5, // Column F
                                EndColumnIndex = 6
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

            // 5. Add thick outer border around each class group
            var borderRequests = new List<Request>();
            foreach (var (startRow, endRow) in classMergeRanges)
            {
                borderRequests.Add(new Request
                {
                    UpdateBorders = new UpdateBordersRequest
                    {
                        Range = new GridRange
                        {
                            SheetId = sheetId,
                            StartRowIndex = startRow,
                            EndRowIndex = endRow + 1,
                            StartColumnIndex = 0, // Column A
                            EndColumnIndex = 6    // Column F (exclusive)
                        },
                        Top = new Border { Style = "SOLID_THICK", Color = GoogleSheetsHelper.BlackColor },
                        Bottom = new Border { Style = "SOLID_THICK", Color = GoogleSheetsHelper.BlackColor },
                        Left = new Border { Style = "SOLID_THICK", Color = GoogleSheetsHelper.BlackColor },
                        Right = new Border { Style = "SOLID_THICK", Color = GoogleSheetsHelper.BlackColor }
                    }
                });
            }

            if (borderRequests.Count > 0)
            {
                var batchBorderRequest = new BatchUpdateSpreadsheetRequest { Requests = borderRequests };
                await _sheetsService.Spreadsheets.BatchUpdate(batchBorderRequest, spreadsheetId)
                    .ExecuteAsync(cancellationToken);
            }
        }
    }

}
