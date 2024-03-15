using TMS.Application.Models;
using TMS.Domain.Enums;

namespace TMS.Application.Interfaces;

public interface IXlsxHelper
{
    string ExcelMimeType { get; }
    string FileExtension { get; }
    MemoryStream WriteTransactionsIntoXlsxFile(IEnumerable<TransactionExportDto> transactions,
        List<TransactionPropertyName> columns, CancellationToken cancellationToken);
}
