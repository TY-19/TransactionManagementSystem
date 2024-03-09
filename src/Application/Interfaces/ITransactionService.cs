using TMS.Application.Models;

namespace TMS.Application.Interfaces;

public interface ITransactionService
{
    Task<CustomResponse> ImportFromCsvAsync(Stream stream);
}
