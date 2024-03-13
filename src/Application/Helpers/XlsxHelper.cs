using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using TMS.Application.Interfaces;
using TMS.Application.Models.Dtos;
using TMS.Domain.Enums;

namespace TMS.Application.Helpers;

public class XlsxHelper(ITransactionPropertyManager propertyManager) : IXlsxHelper
{
    private List<Action<TransactionClientExportDto, int>> actions = null!;
    private XLWorkbook workbook = null!;
    private IXLWorksheet worksheet = null!;
    private int columnNumber = 1;
    private int? userOffset;
    private string? offSetReadable;

    public bool InsertTimeZoneColumn { get; set; }
    public MemoryStream WriteTransactionsIntoXlsxFile(IEnumerable<TransactionClientExportDto> transactions,
        List<TransactionPropertyName> columns, int? userOffset)
    {
        ClearState();
        this.userOffset = userOffset;
        for (int i = 0; i < columns.Count; i++)
        {
            WriteColumnHeader(columnNumber, columns[i]);
            SetPropertyForTheColumn(columns[i]);
            columnNumber++;
        }
        WriteTransactions(transactions);
        worksheet.Columns().AdjustToContents();
        return WorkbookAsAMemoryStream();
    }
    private void ClearState()
    {
        actions = [];
        workbook = new XLWorkbook();
        worksheet = workbook.Worksheets.Add("Transactions");
        columnNumber = 1;
        userOffset = null;
        offSetReadable = null;
    }
    private void WriteColumnHeader(int columnNumber, TransactionPropertyName propertyName)
    {
        worksheet.Cell(1, columnNumber).Value = propertyManager.GetDisplayedName(propertyName);
    }
    private void SetPropertyForTheColumn(TransactionPropertyName propertyName)
    {
        actions.Add(GetWritingAction(columnNumber, propertyName));
    }

    private void WriteTransactions(IEnumerable<TransactionClientExportDto> transactions)
    {
        int row = 2;
        foreach (var transaction in transactions)
        {
            int col = 0;
            while (col < columnNumber - 1)
            {
                actions[col].Invoke(transaction, row);
                col++;
            }
            row++;
        }
    }

    private Action<TransactionClientExportDto, int> GetWritingAction(int column, TransactionPropertyName prop)
    {
        return (t, row) =>
        {
            worksheet.Cell(row, column).Value = GetProperty(t, prop);
            ApplyStyles(worksheet, row, column, prop);
        };
    }
    private XLCellValue GetProperty(TransactionClientExportDto transaction, TransactionPropertyName prop)
    {
        return prop switch
        {
            TransactionPropertyName.TransactionId => transaction.TransactionId,
            TransactionPropertyName.Name => transaction.Name,
            TransactionPropertyName.Email => transaction.Email,
            TransactionPropertyName.Amount => transaction.Amount,
            TransactionPropertyName.TransactionDate => transaction.TransactionDate == null ? ""
                : GetDateTime(transaction.TransactionDate.Value),
            TransactionPropertyName.Offset => GetOffset(transaction.Offset),
            TransactionPropertyName.Latitude => transaction.Latitude,
            TransactionPropertyName.Longitude => transaction.Longitude,
            _ => "",
        };
    }

    private DateTime GetDateTime(DateTimeOffset dateTime)
    {
        if (userOffset == null)
            return dateTime.DateTime;

        return dateTime.UtcDateTime.AddMinutes(userOffset.Value);
    }

    private string GetOffset(string? offset)
    {
        if (userOffset == null)
            return offset ?? string.Empty;

        if (offSetReadable == null)
        {
            char sign = userOffset >= 0 ? '+' : '-';
            int hours = userOffset.Value / 60;
            int minutes = userOffset.Value % 60;
            offSetReadable = $"{sign}{hours:D2}:{minutes:D2}";
        }

        return offSetReadable;
    }

    private static void ApplyStyles(IXLWorksheet worksheet, int row, int column, TransactionPropertyName prop)
    {
        switch (prop)
        {
            case TransactionPropertyName.Amount:
                worksheet.Cell(row, column).Style.NumberFormat.Format = "$0.00";
                break;
            case TransactionPropertyName.TransactionDate:
                worksheet.Cell(row, column).Style.NumberFormat.Format = "dd.mm.yyyy hh:mm:ss";
                break;
        }
    }

    public MemoryStream WorkbookAsAMemoryStream()
    {
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }
}
