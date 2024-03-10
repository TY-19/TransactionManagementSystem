using TMS.Application.Common;
using TMS.Application.Models.Dtos;

namespace TMS.Application.Interfaces;

public interface IXlsxHelper
{
    MemoryStream WriteTransactionsIntoXlsxFile(IEnumerable<TransactionClientExportDto> transactions,
        List<PropertyNames> columns, int? userOffset);
}
