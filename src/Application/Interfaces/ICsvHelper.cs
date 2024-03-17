using TMS.Application.Models;

namespace TMS.Application.Interfaces;

public interface ICsvHelper
{
    /// <summary>
    /// Parses a line in a specific format into a <see cref="TransactionImportDto"/> object.
    /// </summary>
    /// <param name="csvLine">A line of comma-separated values in a specific order.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// An <see cref="OperationResult{TransactionImportDto}"/> with the succeeded property set to true if it is a header, empty or 
    /// the line was parsed  successfully into a <see cref="TransactionImportDto"/> object (placed into Payload);
    /// false if the line cannot be parsed due to an incorrect number or format of values.
    /// </returns>
    Task<OperationResult<TransactionImportDto>> ParseLineAsync(string? csvLine, CancellationToken cancellationToken);
}
