using TMS.Application.Models;

namespace TMS.Application.Interfaces;

public interface ITransactionService
{
    Task<CustomResponse> ImportFromCsvAsync(Stream stream, CancellationToken cancellationToken);

    /// <summary>
    ///     Provides a way to get an Excel file with defined columns of the transactions
    ///     selected and sorted by specified rules.
    /// </summary>
    /// <param name="columns">Comma-separated column names in the desired order.</param>
    /// <param name="sortBy">Name of the column to sort by.</param>
    /// <param name="sortAsc">If set to true, exported transactions are sorted in ascending order;
    ///     otherwise, they are sorted in descending order.</param>
    /// <param name="offset">Time zone offset from UTC in minutes.</param>
    /// <param name="startDate">Lower time limit of the transaction date.</param>
    /// <param name="endDate">Upper time limit of the transaction date.</param>
    /// <param name="cancellationToken">A cancellation token that is used to receive notice of cancellation.</param>
    /// <returns>Xlsx file containing the transactions.</returns>
    Task<MemoryStream> ExportToExcelAsync(string columns, string sortBy, bool sortAsc, TimeZoneDetails? timeZoneDetails,
        DateFilterParameters? startDate, DateFilterParameters? endDate, CancellationToken cancellationToken);
    string GetTransactionsFileName(TimeZoneDetails? timeZoneDetails, DateFilterParameters? startDate,
        DateFilterParameters? endDate);
    string GetFileMimeType();
}
