using TMS.Application.Models;

namespace TMS.Application.Interfaces;

public interface ITransactionService
{
    /// <summary>
    /// Provides a way to import transactions from a .csv file that contains transactions in the format:
    /// transaction_id,name,email,amount,transactionDate,"latitude, longitude"
    /// Example:
    /// T-1-67.63636363636364_0.76,John Doe,john.doe@example.com,$375.39,2024-01-10 01:16:23,"6.602635264, -98.2909591552"
    /// </summary>
    /// <param name="stream">The stream of the CSV file.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>An <see cref="OperationResult"/> that contains the result of the operation
    /// and a list of errors, if any.</returns>
    Task<OperationResult> ImportFromCsvAsync(Stream stream, CancellationToken cancellationToken);

    /// <summary>
    /// Provides a way to export transactions into an Excel file with defined columns,
    /// selected and sorted by specified rules.
    /// </summary>
    /// <param name="columns">Comma-separated column names in the desired order.</param>
    /// <param name="sortBy">Name of the column to sort by.</param>
    /// <param name="sortAsc">
    ///     If set to true, exported transactions are sorted in ascending order;
    ///     otherwise, they are sorted in descending order.
    /// </param>
    /// <param name="timeZoneDetails">
    ///     The <see cref="TimeZoneDetails"/> of the time zone to display time in.
    /// </param>
    /// <param name="startDate">
    ///     The lower time limit of the transaction date in the time zone specified by <paramref name="timeZoneDetails"/>
    ///     or the time zone of the transaction.
    /// </param>
    /// <param name="endDate">
    ///     The upper time limit of the transaction date in the time zone specified by <paramref name="timeZoneDetails"/>
    ///     or the time zone of the transaction.
    /// </param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The Excel file containing the transactions.</returns>
    Task<MemoryStream> ExportToExcelAsync(string columns, string? sortBy, bool sortAsc, TimeZoneDetails? timeZoneDetails,
        DateFilterParameters? startDate, DateFilterParameters? endDate, CancellationToken cancellationToken);

    /// <summary>
    /// Generates the name of the file based on requested details in the format:
    /// transactions_start_date-end-date_time_zone.xlsx
    /// </summary>
    /// <param name="timeZoneDetails">The <see cref="TimeZoneDetails"/> for the file name.</param>
    /// <param name="startDate">The start date for the file name.</param>
    /// <param name="endDate">The end date for the file name.</param>
    /// <returns>The file name.</returns>
    string GetTransactionsFileName(TimeZoneDetails? timeZoneDetails, DateFilterParameters? startDate,
        DateFilterParameters? endDate);

    /// <summary>
    /// Allows to get the MIME type of the .xlsx file.
    /// </summary>
    /// <returns>The MIME Type.</returns>
    string GetExcelFileMimeType();

    /// <summary>
    /// Allows to get transactions for the specified period of time.
    /// </summary>
    /// <param name="dateFrom">The start of the period.</param>
    /// <param name="dateTo">The end of the period.</param>
    /// <returns>List of transactions in the specified time period.</returns>
    Task<IEnumerable<TransactionDto>> GetForTimePeriodAsync(DateOnly dateFrom, DateOnly dateTo);
}
