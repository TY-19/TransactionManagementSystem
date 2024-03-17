using TMS.Application.Models;
using TMS.Domain.Enums;

namespace TMS.Application.Interfaces;

public interface IXlsxHelper
{
    /// <summary>
    /// Gets the MIME type for Excel files.
    /// </summary>
    string ExcelMimeType { get; }

    /// <summary>
    /// Gets the file extension for Excel files.
    /// </summary>
    string ExcelFileExtension { get; }

    /// <summary>
    /// Creates an Excel (.xlsx) file containing specified columns of transactions.
    /// </summary>
    /// <param name="transactions">The transactions to write.</param>
    /// <param name="columns">The properties to write.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A memory stream containing the Excel file with the written transactions.</returns>
    MemoryStream WriteTransactionsIntoXlsxFile(IEnumerable<TransactionExportDto> transactions,
        List<TransactionPropertyName> columns, CancellationToken cancellationToken);
}
