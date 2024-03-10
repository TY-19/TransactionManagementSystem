using TMS.Application.Models;

namespace TMS.Application.Interfaces;

public interface ITransactionService
{
    Task<CustomResponse> ImportFromCsvAsync(Stream stream);
    Task<MemoryStream> ExportToExcelAsync(string fields, int? userOffset);
}
