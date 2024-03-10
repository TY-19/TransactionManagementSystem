using TMS.Application.Models;
using TMS.Application.Models.Dtos;

namespace TMS.Application.Interfaces;

public interface ICsvParser
{
    Task<CustomResponse<TransactionDto>> TryParseLineAsync(string? cssLine);
}
