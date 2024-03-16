using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using TMS.Application.Interfaces;
using TMS.Application.Models;
using TMS.Domain.Enums;

namespace TMS.Application.Helpers;

public class XlsxHelper(
    ITransactionPropertyManager propertyManager
    ) : IXlsxHelper
{
    private List<Action<TransactionExportDto, int>> actions = null!;
    private XLWorkbook workbook = null!;
    private IXLWorksheet worksheet = null!;
    private int columnNumber = 1;

    public string ExcelMimeType => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public string FileExtension => ".xlsx";
    /// <inheritdoc cref="IXlsxHelper.WriteTransactionsIntoXlsxFile(IEnumerable{TransactionExportDto}, List{TransactionPropertyName}, CancellationToken);"/>
    public MemoryStream WriteTransactionsIntoXlsxFile(IEnumerable<TransactionExportDto> transactions,
        List<TransactionPropertyName> columns, CancellationToken cancellationToken)
    {
        ClearState();
        for (int i = 0; i < columns.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            WriteColumnHeader(columnNumber, columns[i]);
            SetPropertyForTheColumn(columns[i]);
            columnNumber++;
        }
        WriteTransactions(transactions, cancellationToken);
        worksheet.Columns().AdjustToContents();
        return WorkbookAsAMemoryStream();
    }
    private void ClearState()
    {
        actions = [];
        workbook = new XLWorkbook();
        worksheet = workbook.Worksheets.Add("Transactions");
        columnNumber = 1;
    }
    private void WriteColumnHeader(int columnNumber, TransactionPropertyName propertyName)
    {
        worksheet.Cell(1, columnNumber).Value = propertyManager.GetDisplayedName(propertyName);
    }
    private void SetPropertyForTheColumn(TransactionPropertyName propertyName)
    {
        actions.Add(GetWritingAction(columnNumber, propertyName));
    }

    private void WriteTransactions(IEnumerable<TransactionExportDto> transactions, CancellationToken cancellationToken)
    {
        int row = 2;
        foreach (var transaction in transactions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int col = 0;
            while (col < columnNumber - 1)
            {
                actions[col].Invoke(transaction, row);
                col++;
            }
            row++;
        }
    }

    private Action<TransactionExportDto, int> GetWritingAction(int column, TransactionPropertyName prop)
    {
        return (t, row) =>
        {
            worksheet.Cell(row, column).Value = GetProperty(t, prop);
            ApplyStyles(worksheet, row, column, prop);
        };
    }
    private static XLCellValue GetProperty(TransactionExportDto transaction, TransactionPropertyName prop)
    {
        return prop switch
        {
            TransactionPropertyName.TransactionId => transaction.TransactionId,
            TransactionPropertyName.Name => transaction.Name,
            TransactionPropertyName.Email => transaction.Email,
            TransactionPropertyName.Amount => transaction.Amount,
            TransactionPropertyName.TransactionDate => transaction.TransactionDate.HasValue
                ? transaction.TransactionDate.Value.DateTime : "",
            TransactionPropertyName.Offset => transaction.Offset,
            TransactionPropertyName.Latitude => transaction.Latitude,
            TransactionPropertyName.Longitude => transaction.Longitude,
            _ => "",
        };
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
