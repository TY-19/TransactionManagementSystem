using MediatR;
using TMS.Application.Models;

namespace TMS.Application.Queries.TransactionClient.GetTransactionsClients;

public class GetTransactionsClientsQuery : IRequest<IEnumerable<TransactionExportDto>>
{
    public List<string> ColumnNames { get; set; } = [];
    public string? SortBy { get; set; }
    public bool SortAsc { get; set; }
    public bool UseUserTimeZone { get; set; }
    public DateFilterParameters? StartDate { get; set; }
    public DateFilterParameters? EndDate { get; set; }
    public string StartDateOffset { get; set; } = string.Empty;
    public string EndDateOffset { get; set; } = string.Empty;
}
