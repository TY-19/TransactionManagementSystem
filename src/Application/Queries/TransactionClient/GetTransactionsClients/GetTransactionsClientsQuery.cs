using MediatR;
using TMS.Application.Models;

namespace TMS.Application.Queries.TransactionClient.GetTransactionsClients;

public class GetTransactionsClientsQuery : IRequest<IEnumerable<TransactionExportDto>>
{
    public IEnumerable<string> ColumnNames { get; set; } = [];
    public string? SortBy { get; set; }
    public bool SortAsc { get; set; }
    public bool UseUserTimeZone { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public string DateFromOffset { get; set; } = string.Empty;
    public string DateToOffset { get; set; } = string.Empty;
}
