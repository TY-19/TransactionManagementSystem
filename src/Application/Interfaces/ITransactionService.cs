using TMS.Application.Models;

namespace TMS.Application.Interfaces;

public interface ITransactionService
{
    Task<CustomResponse> ImportFromCsvAsync(Stream stream);
    Task<MemoryStream> ExportToExcelAsync(string fields, string sortBy, bool sortAsc, int? userOffset,
        DateFilterParameters? startDate, DateFilterParameters? endDate);
    string GetExcelFileName(int? userOffset, DateFilterParameters? startDate, DateFilterParameters? endDate);
}
