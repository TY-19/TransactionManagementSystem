using TMS.Application.Models;

namespace TMS.Application.Interfaces;

public interface ICsvParser
{
    Task<CustomResponse<TransactionImportDto>> ParseLineAsync(string? cssLine, CancellationToken cancellationToken);
}
