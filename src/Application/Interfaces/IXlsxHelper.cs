using TMS.Application.Models.Dtos;
using TMS.Domain.Enums;

namespace TMS.Application.Interfaces;

public interface IXlsxHelper
{
    MemoryStream WriteTransactionsIntoXlsxFile(IEnumerable<TransactionClientExportDto> transactions,
        List<TransactionPropertyName> columns, int? userOffset);
}
