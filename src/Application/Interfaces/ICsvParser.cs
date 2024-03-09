using TMS.Application.Models;

namespace TMS.Application.Interfaces;

public interface ICsvParser
{
    Task<CustomResponse<TransactionDto>> TryParseLineAsync(string? cssLine);
}
