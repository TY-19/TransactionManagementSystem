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
    IIpService ipService,
    ITimeZoneService timeZoneService
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
    [Route("import")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ImportTransactionsFromCsv(IFormFile csvFile)
    {
        if (!csvFile.FileName.EndsWith(".csv") || csvFile.ContentType != "text/csv")
            return BadRequest("Invalid file format");

        using Stream stream = csvFile.OpenReadStream();
        var result = await transactionService.ImportFromCsvAsync(stream, Request.HttpContext.RequestAborted);
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
    /// <param name="useUserTimeZone">
    ///     If set to true, the date and time values are adjusted to display in the time zone 
    ///     of the current user, determined based on their IP address.
    ///     If set to false, each transaction's time is displayed in its respective time zones.
    ///     This setting also defines the time zone to use when combined with properties that 
    ///     require filtering by date.
    /// </param>
    /// <param name="timeZoneIanaName">
    ///     Full IANA time zone name.
    ///     If set the date and time values are adjusted to display in the time of this zone.
    ///     Takes precedence over userUserTimeZone flag. When combined with properties that
    ///     require filtering by date the time of the specified time zone is used.
    ///     Tp get all available time zones see https://timeapi.io/api/TimeZone/AvailableTimeZones
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
    /// <response code="400">Error message.</response>
    [Route("export")]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ExportTransactionsToExcel(string columns = defaultColumnsToExport,
        string sortBy = defaultSortBy, bool sortAsc = true, bool useUserTimeZone = false,
        string? timeZoneIanaName = null, int? startYear = null, int? startMonth = null,
        int? startDay = null, int? endYear = null, int? endMonth = null, int? endDay = null)
    {
        var tzResponse = await ConfigureTimeZoneAsync(timeZoneIanaName, useUserTimeZone);
        if (!tzResponse.Succeeded)
            return BadRequest(tzResponse.Message);

        TimeZoneDetails? timeZone = tzResponse.Payload;
        DateFilterParameters? startDate = DateFilterParameters.CreateFilterParameters(startYear, startMonth, startDay, true);
        DateFilterParameters? endDate = DateFilterParameters.CreateFilterParameters(endYear, endMonth, endDay, false);

        string fileName = transactionService.GetTransactionsFileName(timeZone, startDate, endDate);
        string fileType = transactionService.GetFileMimeType();
        MemoryStream fileStream = await transactionService.ExportToExcelAsync(columns, sortBy, sortAsc,
            timeZone, startDate, endDate, Request.HttpContext.RequestAborted);
        return File(fileStream, fileType, fileName);
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
    /// <response code="400">Error message.</response>
    [Route("export/clients_tz/2023")]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ExportTransactionsToExcel2023LocalTime()
    {
        return await ExportTransactionsToExcel(startYear: 2023, endYear: 2023);
    }

    /// <summary>
    ///     Allows exporting transactions completed in January 2024, adjusted to the local time
    ///     of each client.
    /// </summary>
    /// <remarks>
    ///     This is a predefined endpoint tailored to the specific task requirement of providing
    ///     transactions for January 2024 in clients' respective time zones.
    /// 
    ///     For further customization of queries, use the 'api/transactions/export/clients' endpoint.
    /// </remarks>
    /// <response code="200">Returns the requested file with transactions.</response>
    /// <response code="400">Error message.</response>
    [Route("export/clients_tz/2024/01")]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
    /// <response code="400">Error message.</response>
    [Route("export/my_tz/2023")]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ExportTransactionsToExcel2023UserTime()
    {
        return await ExportTransactionsToExcel(useUserTimeZone: true, startYear: 2023, endYear: 2023);
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
    /// <response code="400">Error message.</response>
    [Route("export/my_tz/2024/01")]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ExportTransactionsToExcel202401UserTime()
    {
        return await ExportTransactionsToExcel(useUserTimeZone: true,
            startYear: 2024, startMonth: 01, endYear: 2024, endMonth: 01);
    }

    private async Task<CustomResponse<TimeZoneDetails?>> ConfigureTimeZoneAsync(string? timeZoneIanaName, bool useUserTimeZone)
    {
        if (timeZoneIanaName != null)
        {
            var tzsResponse = await timeZoneService.GetTimeZoneByIanaNameAsync(timeZoneIanaName, Request.HttpContext.RequestAborted);
            if (!tzsResponse.Succeeded)
                return new CustomResponse<TimeZoneDetails?>(false, "Specified IANA time zone name is invalid.");

            return new CustomResponse<TimeZoneDetails?>(true, tzsResponse.Payload);
        }
        else if (useUserTimeZone)
        {
            string? requestIp = Request.HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString();
            var ipsResponse = await ipService.GetIpAsync(requestIp, Request.HttpContext.RequestAborted);
            if (!ipsResponse.Succeeded)
                return new CustomResponse<TimeZoneDetails?>(false, ipsResponse.Message ?? "IP cannot be determined.");

            string ipToUse = ipsResponse.Payload!;

            var tzsResponse = await timeZoneService.GetTimeZoneByIpAsync(ipToUse, Request.HttpContext.RequestAborted);
            if (!tzsResponse.Succeeded)
                return new CustomResponse<TimeZoneDetails?>(false, "Cannot get timezone for the user IP");

            return new CustomResponse<TimeZoneDetails?>(true, tzsResponse.Payload);
        }
        else
        {
            return new CustomResponse<TimeZoneDetails?>(true);
        }
    }
}
