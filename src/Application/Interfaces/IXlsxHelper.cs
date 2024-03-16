using TMS.Application.Models;
using TMS.Domain.Enums;

namespace TMS.Application.Interfaces;

public interface IXlsxHelper
{
    string ExcelMimeType { get; }
    string FileExtension { get; }
    /// <summary>
    ///     Creates an xlsx file containing specified columns of transactions.
    /// </summary>
    /// <param name="transactions">Transactions to write.</param>
    /// <param name="columns">Properties to write.</param>
    /// <param name="cancellationToken">A cancellation token that is used to receive notice of cancellation.</param>
    /// <returns>Memory stream of xlsx file with written transactions.</returns>
    MemoryStream WriteTransactionsIntoXlsxFile(IEnumerable<TransactionExportDto> transactions,
        List<TransactionPropertyName> columns, CancellationToken cancellationToken);
}
