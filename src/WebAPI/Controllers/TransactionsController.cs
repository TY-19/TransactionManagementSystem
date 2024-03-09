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
    /// <returns></returns>
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
}
