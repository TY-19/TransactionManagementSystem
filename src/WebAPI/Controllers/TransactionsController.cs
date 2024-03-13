using Microsoft.AspNetCore.Mvc;
using System.Net;
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
    IIpService ipService
    ) : ControllerBase
{
    private const string defaultColumnsToExport = "transactionId,name,email,amount,transactionDate,offset,latitude,longitude";
    private const string defaultSortBy = "transactionId";

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
    /// <response code="204">Transactions were successfully imported.</response>
    /// <response code="400">Transactions cannot be imported.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ImportTransactionsFromCsv(IFormFile csvFile)
    {
        if (!csvFile.FileName.EndsWith(".csv") || csvFile.ContentType != "text/csv")
            return BadRequest("Invalid file format");

        using Stream stream = csvFile.OpenReadStream();
        var result = await transactionService.ImportFromCsvAsync(stream);
        return result.Succeeded ? NoContent() : BadRequest(result.Message);
    }

    /// <summary>
    ///     Allows exporting transactions into an .xlsx file while offering extensive
    ///     customization options for specifying the transaction range.
    /// </summary>
    /// <param name="columns">
    ///     Comma-separated column names in the desired order.
    ///     Allowed columns (case insensitive): transactionId, name, email, amount,
    ///     transactionDate, offset, latitude, longitude.
    /// </param>
    /// <param name="sortBy">
    ///     Name of the column to sort by (transactionId by default). 
    ///     Allowed columns (case insensitive): transactionId, name, email, amount,
    ///     transactionDate, offset, latitude, longitude.
    ///     Sorting order is defined by the value of the sortAsc parameter (ascending by default).
    /// </param>
    /// <param name="sortAsc">
    ///     If set to true, exported transactions are sorted in ascending order;
    ///     otherwise, they are sorted in descending order.
    ///     Column to sort is specified in the sortBy parameter.
    /// </param>
    /// <param name="timeInUserTimeZone">
    ///     If set to true, the date and time values are adjusted to display in the time zone 
    ///     of the current user, determined based on their IP address.
    ///     If set to false, each transaction's time is displayed in its respective time zone,
    ///     independent of the user's time zone.
    ///     This setting also defines the time zone to use when combined with properties that 
    ///     require filtering by date.
    /// </param>
    /// <param name = "offset" >
    ///     For TESTING PURPOSES. Time zone offset from UTC in minutes.
    ///     If set, timeInUserTimeZone flag is ignored and date and time values are displayed
    ///     for time zones with the specified offset
    /// </param>
    /// <param name="startYear">
    ///     If set, transactions with transactionDate in this year (starting Jan. 1)
    ///     or following years will be returned.
    ///     Can be combined with startMonth and startDay to set a specific start date.
    ///     Can be combined with endYear, endMonth, endDay to narrow the date range.
    /// </param>
    /// <param name="startMonth">
    ///     Specify the month of the startYear to narrow the filtering range.
    ///     Has no effect if startYear is not provided.
    ///     Can be combined with startDay to set a specific start day (otherwise starts with
    ///     the first day of the month).
    /// </param>
    /// <param name="startDay">
    ///     Specify the day of the startMonth to narrow the filtering range.
    ///     Has no effect if startYear is not provided.
    /// </param>
    /// <param name="endYear">
    ///     If set, transactions with transactionDate in this year (ending Dec. 31) or previous
    ///     years will be returned.
    ///     Can be combined with endMonth and endDay to set a specific date.
    ///     Can be combined with startYear, startMonth, startDay to narrow the date range.
    /// </param> 
    /// <param name="endMonth">
    ///     Specify the month of the endYear to narrow the filtering range.
    ///     Has no effect if endYear is not provided.
    ///     Can be combined with endDay to set a specific end day (otherwise ends with the last
    ///     day of the month).
    /// </param>
    /// <param name="endDay">
    ///     Specify the day of the endMonth to narrow the filtering range.
    ///     Has no effect if endYear is not provided.
    /// </param>
    /// <remarks>
    ///     If timeInUserTimeZone is set to true, the date scope specified in startYear, startMonth,
    ///     startDay, endYear, endMonth, endDay parameters is calculated for the user's time zone.
    ///     Otherwise, the date scope is defined for the time zone of each transaction separately.
    ///     The offset parameter overrides time zone used for filtering and displaying transaction
    ///     data.
    /// </remarks>
    /// <response code="200">Returns the requested file with transactions.</response>
    [HttpGet]
    [Route("export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> ExportTransactionsToExcel(
        string columns = defaultColumnsToExport, string sortBy = defaultSortBy, bool sortAsc = true,
        bool timeInUserTimeZone = false, int? offset = null,
        int? startYear = null, int? startMonth = null, int? startDay = null,
        int? endYear = null, int? endMonth = null, int? endDay = null)
    {
        if (timeInUserTimeZone && offset == null)
        {
            IPAddress? ipAddress = Request.HttpContext.Connection.RemoteIpAddress;
            offset = await ipService.GetTimeZoneOffsetInMinutesAsync(ipAddress?.MapToIPv4().ToString() ?? "");
        }

        DateFilterParameters? startDate = DateFilterParameters.CreateFilterParameters(startYear, startMonth, startDay, true);
        DateFilterParameters? endDate = DateFilterParameters.CreateFilterParameters(endYear, endMonth, endDay, false);

        string fileName = transactionService.GetExcelFileName(offset, startDate, endDate);
        
        MemoryStream stream = await transactionService.ExportToExcelAsync(columns, sortBy, sortAsc, offset, startDate, endDate);
        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>
    ///     Allows exporting transactions completed in 2023, adjusted to the local time of each client.
    /// </summary>
    /// <remarks>
    ///     This is a predefined endpoint tailored to the specific task requirement of providing
    ///     transactions for 2023 in clients' respective time zones.
    /// 
    ///     For further customization of queries, use the 'api/transactions/export/clients' endpoint.
    /// </remarks>
    /// <response code="200">Returns the requested file with transactions.</response>
    [HttpGet]
    [Route("export/clients_tz/2023")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> ExportTransactionsToExcel2023LocalTime()
    {
        return await ExportTransactionsToExcel(startYear: 2023, endYear: 2023);
    }

    /// <summary>
    ///     Allows exporting transactions completed in January 2024, adjusted to the local time of each client.
    /// </summary>
    /// <remarks>
    ///     This is a predefined endpoint tailored to the specific task requirement of providing
    ///     transactions for January 2024 in clients' respective time zones.
    /// 
    ///     For further customization of queries, use the 'api/transactions/export/clients' endpoint.
    /// </remarks>
    /// <response code="200">Returns the requested file with transactions.</response>
    [HttpGet]
    [Route("export/clients_tz/2024/01")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> ExportTransactionsToExcel202401ClientsTime()
    {
        return await ExportTransactionsToExcel(startYear: 2024, startMonth: 01, endYear: 2024, endMonth: 01);
    }

    /// <summary>
    ///     Allows exporting transactions completed in 2023, adjusted to the time zone
    ///     of the current API user based on their IP address.
    /// </summary>
    /// <remarks>
    ///     This is a predefined endpoint tailored to the specific task requirement of providing
    ///     transactions for 2023 in the time zone of the current API user.
    /// 
    ///     For further customization of queries, use the 'api/transactions/export/clients' endpoint.
    /// </remarks>
    /// <response code="200">Returns the requested file with transactions.</response>
    [HttpGet]
    [Route("export/my_tz/2023")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> ExportTransactionsToExcel2023UserTime()
    {
        return await ExportTransactionsToExcel(timeInUserTimeZone: true, startYear: 2023, endYear: 2023);
    }

    /// <summary>
    ///     Allows exporting transactions completed in the January 2024, adjusted to the time zone
    ///     of the current API user based on their IP address.
    /// </summary>
    /// <remarks>
    ///     This is a predefined endpoint tailored to the specific task requirement of providing
    ///     transactions for the year January 2024 in the time zone of the current API user.
    /// 
    ///     For further customization of queries, use the 'api/transactions/export/clients' endpoint.
    /// </remarks>
    /// <response code="200">Returns the requested file with transactions.</response>
    [HttpGet]
    [Route("export/my_tz/2024/01")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> ExportTransactionsToExcel202401UserTime()
    {
        return await ExportTransactionsToExcel(timeInUserTimeZone: true, startYear: 2024, startMonth: 01, endYear: 2024, endMonth: 01);
    }    
}
