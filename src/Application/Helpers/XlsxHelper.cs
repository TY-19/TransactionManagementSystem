using ClosedXML.Excel;
using TMS.Application.Interfaces;
using TMS.Application.Models;
using TMS.Domain.Enums;

namespace TMS.Application.Helpers;

public class XlsxHelper(
    ITransactionPropertyHelper propertyManager
    ) : IXlsxHelper
{
    private List<Action<TransactionExportDto, int>> _actions = null!;
    private XLWorkbook _workbook = null!;
    private IXLWorksheet _worksheet = null!;
    private int _columnNumber = 1;

    /// <inheritdoc cref="IXlsxHelper.ExcelMimeType"/>
    public string ExcelMimeType => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    /// <inheritdoc cref="IXlsxHelper.ExcelFileExtension"/>
    public string ExcelFileExtension => ".xlsx";

    /// <inheritdoc cref="IXlsxHelper.WriteTransactionsIntoXlsxFile(IEnumerable{TransactionExportDto}, List{TransactionPropertyName}, CancellationToken);"/>
    public MemoryStream WriteTransactionsIntoXlsxFile(IEnumerable<TransactionExportDto> transactions,
        List<TransactionPropertyName> columns, CancellationToken cancellationToken)
    {
        ClearState();
        for (int i = 0; i < columns.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            WriteColumnHeader(_columnNumber, columns[i]);
            SetPropertyForTheColumn(columns[i]);
            _columnNumber++;
        }
        WriteTransactions(transactions, cancellationToken);
        _worksheet.Columns().AdjustToContents();
        return WorkbookAsAMemoryStream(cancellationToken);
    }
    private void ClearState()
    {
        _actions = [];
        _workbook = new XLWorkbook();
        _worksheet = _workbook.Worksheets.Add("Transactions");
        _columnNumber = 1;
    }
    private void WriteColumnHeader(int columnNumber, TransactionPropertyName propertyName)
    {
        _worksheet.Cell(1, columnNumber).Value = propertyManager.GetDisplayedName(propertyName);
    }
    private void SetPropertyForTheColumn(TransactionPropertyName propertyName)
    {
        _actions.Add(GetWritingAction(_columnNumber, propertyName));
    }

    private void WriteTransactions(IEnumerable<TransactionExportDto> transactions,
        CancellationToken cancellationToken)
    {
        int row = 2;
        foreach (TransactionExportDto transaction in transactions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int col = 0;
            while (col < _columnNumber - 1)
            {
                _actions[col].Invoke(transaction, row);
                col++;
            }
            row++;
        }
    }

    private Action<TransactionExportDto, int> GetWritingAction(int column, TransactionPropertyName prop)
    {
        return (t, row) =>
        {
            _worksheet.Cell(row, column).Value = GetProperty(t, prop);
            ApplyStyles(_worksheet, row, column, prop);
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

    public MemoryStream WorkbookAsAMemoryStream(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var stream = new MemoryStream();
        _workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }
}
