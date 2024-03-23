using Microsoft.AspNetCore.Mvc;
using TMS.Application.Interfaces;
using TMS.Application.Models;

namespace TMS.WebAPI.Controllers;

/// <summary>
///     Controller providing a range of endpoints for working with transactions.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class TransactionsController(
    ITransactionService transactionService,
    ITimeZoneService timeZoneService
    ) : ControllerBase
{
    private const string defaultColumnsToExport = "transactionId,name,email,amount,transactionDate,offset,latitude,longitude";

    /// <summary>
    ///     Allows importing transactions from a CSV file.
    /// </summary>
    /// <param name="csvFile">
    ///     CSV file containing a list of transactions.
    /// </param>
    /// <remarks>
    ///     Example of a single transaction record in CSV format:
    /// 
    ///     T-1-67.63636363636364_0.76,John Doe,john.doe@example.com,$375.39,2024-01-10 01:16:23,"6.602635264, -98.2909591552"
    /// </remarks>
    /// <response code="200">Statistics of the import process.</response>
    /// <response code="400">Transactions cannot be imported.</response>
    [Route("import")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ImportTransactionsFromCsv(IFormFile csvFile)
    {
        if (!csvFile.FileName.EndsWith(".csv") || csvFile.ContentType != "text/csv")
            return BadRequest("Invalid file format");

        using Stream stream = csvFile.OpenReadStream();
        return Ok(await transactionService.ImportFromCsvAsync(stream, Request.HttpContext.RequestAborted));
    }

    /// <summary>
    /// Allows exporting transactions into an .xlsx file while offering extensive  customization
    /// options for specifying the transaction range.
    /// </summary>
    /// <param name="columns">
    /// Comma-separated column names in the desired order. Allowed columns (case insensitive):
    /// transactionId, name, email, amount, transactionDate, offset, latitude, longitude.
    /// </param>
    /// <param name="sortBy">
    /// Name of the column to sort by. 
    /// Allowed columns (case insensitive): transactionId, name, email, amount, transactionDate,
    /// offset, latitude, longitude.
    /// Sorting order is defined by the value of the <paramref name="sortAsc"/>(ascending by default).
    /// </param>
    /// <param name="sortAsc">
    /// If set to true, exported transactions are sorted in ascending order;
    /// otherwise, they are sorted in descending order.
    /// Column to sort is specified in the <paramref name="sortBy"/>.
    /// </param>
    /// <param name="useUserTimeZone">
    /// If set to true, the date and time values are adjusted to display in the time zone 
    /// of the current user, determined based on their IP address.
    /// If set to false, each transaction's time is displayed in its respective time zones.
    /// This setting also defines the time zone to use when combined with properties that 
    /// require filtering by date.
    /// </param>
    /// <param name="timeZoneIanaName">
    /// Full IANA time zone name.
    /// If set, the date and time values are adjusted to display in the time of this zone.
    /// Takes precedence over <paramref name="useUserTimeZone"/> flag.
    /// When combined with properties that require filtering by date, the time of the specified
    /// time zone is used. To get all available time zones see
    /// <code>https://timeapi.io/api/TimeZone/AvailableTimeZones</code>
    /// </param>
    /// <param name="startDate">
    /// Date in format yyyy-MM-dd.
    /// If set, transactions with transactionDate starting on this or following dates will be returned.
    /// Can be combined with <paramref name="endDate"/> to narrow the date range.
    /// </param>
    /// <param name="endDate">
    /// Date in format yyyy-MM-dd.
    /// If set, transactions with transactionDate on this or previous dates will be returned.
    /// Can be combined with <paramref name="startDate"/> to narrow the date range.
    /// </param> 
    /// <remarks>
    /// If timeInUserTimeZone is set to true, the date scope specified in startYear, startMonth,
    /// startDay, endYear, endMonth, endDay parameters is calculated for the user's time zone.
    /// Otherwise, the date scope is defined for the time zone of each transaction separately.
    /// The offset parameter overrides the time zone used for filtering and displaying transaction
    /// data.
    /// </remarks>
    /// <response code="200">Returns the requested file with transactions.</response>
    /// <response code="400">Error message.</response>
    [Route("export")]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ExportTransactionsToExcel(string columns = defaultColumnsToExport,
        string? sortBy = null, bool sortAsc = true, bool useUserTimeZone = false,
        string? timeZoneIanaName = null, DateOnly? startDate = null, DateOnly? endDate = null)
    {
        OperationResult<TimeZoneDetails> tzsResponse = await timeZoneService.GetTimeZoneAsync(
            timeZoneIanaName, useUserTimeZone, Request.HttpContext.Connection.RemoteIpAddress,
            Request.HttpContext.RequestAborted);
        if (!tzsResponse.Succeeded)
        {
            return BadRequest(tzsResponse.Message);
        }

        TimeZoneDetails? timeZone = tzsResponse.Payload;
        string fileName = transactionService.GetTransactionsFileName(timeZone, startDate, endDate);
        string fileType = transactionService.GetExcelFileMimeType();
        MemoryStream fileStream = await transactionService
            .ExportToExcelAsync(columns, sortBy, sortAsc, timeZone, startDate, endDate,
            Request.HttpContext.RequestAborted);

        return File(fileStream, fileType, fileName);
    }

    /// <summary>
    /// Allows to get transactions with the transaction date in the specified range.
    /// </summary>
    /// <param name="dateFrom">Start of the range to get transactions. Format yyyy-MM-dd</param>
    /// <param name="dateTo">End of the range to get transactions. Format yyyy-MM-dd</param>
    /// <returns>List of transactions.</returns>
    /// <response code="200">Returns the list of transactions in the specified range.</response>
    /// <remarks>
    ///     Example of a request:
    ///     
    ///     /api/transactions?dateFrom=2024-01-10&amp;dateTo=2024-02-22
    /// </remarks>
    [Route("")]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TransactionDto>>> GetTransactionsForTimePeriod(
        DateOnly dateFrom, DateOnly dateTo)
    {
        return Ok(await transactionService.GetForTimePeriodAsync(dateFrom, dateTo));
    }
}
