using ClosedXML.Excel;
using TMS.Application.Common;
using TMS.Application.Interfaces;
using TMS.Application.Models.Dtos;

namespace TMS.Application.Helpers;

public class XlsxHelper : IXlsxHelper
{
    private List<Action<TransactionClientExportDto, int>> actions = null!;
    private XLWorkbook workbook = null!;
    private IXLWorksheet worksheet = null!;
    private int columnNumber = 1;
    private int? userOffset;
    private string? offSetReadable;

    public bool InsertTimeZoneColumn { get; set; }
    public MemoryStream WriteTransactionsIntoXlsxFile(IEnumerable<TransactionClientExportDto> transactions,
        List<PropertyNames> columns, int? userOffset)
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
    private void WriteColumnHeader(int columnNumber, PropertyNames propertyName)
    {
        worksheet.Cell(1, columnNumber).Value = propertyName.DisplayedName;
    }
    private void SetPropertyForTheColumn(PropertyNames propertyName)
    {
        actions.Add(SetSelectedProperty(columnNumber, propertyName.NormalizedName));
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

    private Action<TransactionClientExportDto, int> SetSelectedProperty(int column, string propertyName)
    {
        Action<TransactionClientExportDto, int> action = (t, row) => { };
        switch (propertyName)
        {
            case string transactionId when transactionId == TransactionPropertiesNames.TransactionId.NormalizedName:
                action = (t, row) => worksheet.Cell(row, column).Value = t.TransactionId;
                break;
            case string transactionIdShort when transactionIdShort == TransactionPropertiesNames.TransactionIdShort.NormalizedName:
                action = (t, row) => worksheet.Cell(row, column).Value = t.TransactionId;
                break;
            case string name when name == TransactionPropertiesNames.Name.NormalizedName:
                action = (t, row) => worksheet.Cell(row, column).Value = t.Name;
                break;
            case string email when email == TransactionPropertiesNames.Email.NormalizedName:
                action = (t, row) => worksheet.Cell(row, column).Value = t.Email;
                break;
            case string amount when amount == TransactionPropertiesNames.Amount.NormalizedName:
                action = (t, row) => worksheet.Cell(row, column).Value = $"${(t.Amount == null ? 0 : t.Amount.Value)}";
                break;
            case string transactionDate when transactionDate == TransactionPropertiesNames.TransactionDate.NormalizedName:
                action = (t, row) => worksheet.Cell(row, column).Value = t.TransactionDate == null ? "" : GetDateTime(t.TransactionDate.Value);
                break;
            case string offset when offset == TransactionPropertiesNames.Offset.NormalizedName:
                action = (t, row) => worksheet.Cell(row, column).Value = GetOffset(t.Offset);
                break;
            case string latitude when latitude == TransactionPropertiesNames.Latitude.NormalizedName:
                action = (t, row) => worksheet.Cell(row, column).Value = t.Latitude;
                break;
            case string longitude when longitude == TransactionPropertiesNames.Longitude.NormalizedName:
                action = (t, row) => worksheet.Cell(row, column).Value = t.Longitude;
                break;
        }
        return action;
    }
    public MemoryStream WorkbookAsAMemoryStream()
    {
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    private DateTime GetDateTime(DateTimeOffset dateTime)
    {
        if (userOffset == null)
            return dateTime.LocalDateTime;

        return dateTime.UtcDateTime.AddMinutes(userOffset.Value);
    }

    private string GetOffset(string? offset)
    {
        if (userOffset == null)
            return offset ?? string.Empty;

        if (offSetReadable == null)
        {
            char sign = userOffset > 0 ? '+' : '-';
            int hours = userOffset.Value / 60;
            int minutes = userOffset.Value % 60;
            offSetReadable = $"{sign}{hours:D2}:{minutes:D2}";
        }

        return offSetReadable;
    }
}
