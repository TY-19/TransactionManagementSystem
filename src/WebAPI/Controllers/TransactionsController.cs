using Microsoft.AspNetCore.Mvc;
using TMS.Application.Interfaces;

namespace TMS.WebAPI.Controllers;

/// <summary>
/// Controller provides a range of endpoints to work with transactions
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class TransactionsController(ITransactionService transactionService) : ControllerBase
{
    /// <summary>
    /// Allows to import transactions from a CSV file.
    /// </summary>
    /// <param name="csvFile">CSV file that contains list of transactions</param>
    /// <remarks>
    ///     Example of a single transaction record in CSV format:
    ///     
    ///     T-1-67.63636363636364_0.76,John Doe,john.doe.edu,$375.39,2024-01-10 01:16:23,"6.602635264, -98.2909591552"
    /// </remarks>
    /// <response code="204">Transactions were successfully imported</response>
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

    private const string defaultFieldsToExport = "transactionId,name,email,amount,transactionDate,offset";
    /// <summary>
    /// Allows to export specified field of transactions into an .xlsx file
    /// </summary>
    /// <param name="fields">Field names separated by ',' in desirable order.</param>
    /// <param name="timeInUsersTimeZone">
    ///     If set to true date and time values will be displayed in the time zone of the current user
    ///     that is determined based on their IP. Otherwise time is displayed in their own  time zones
    /// </param>
    /// <param name="offset">
    ///     For the TESTING PURPOSES. Time zone offset from UTC in the minutes.
    ///     If set then date and time values will be displayed for the time zones with such an offset.
    /// </param>
    /// <remarks>
    ///     You can specify the fields names (case insensitive) to export separated by comma:
    ///     transactionId,name,email,amount,transactionDate,offset,latitude,longitude
    ///     
    ///     Order of the columns in Excel will be in the same order as order of specified fields
    /// </remarks>
    /// <response code="200">Returns the requested file with specified field (default if not specified)</response>
    [HttpGet]
    [Route("export")]
    public async Task<ActionResult> ExportTransactionsToExcel(string fields = defaultFieldsToExport,
        bool timeInUsersTimeZone = false, int? offset = null)
    {
        if (timeInUsersTimeZone && offset == null)
        {
            // TODO: Implement service to determine current user offset based on IP
            offset = 0;
        }

        MemoryStream stream = await transactionService.ExportToExcelAsync(fields, offset);
        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "transactions.xlsx");
    }
}
