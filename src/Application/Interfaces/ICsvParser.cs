using TMS.Application.Models;

namespace TMS.Application.Interfaces;

public interface ICsvParser
{
    Task<CustomResponse<TransactionImportDto>> TryParseLineAsync(string? cssLine, CancellationToken cancellationToken);
}
