using MediatR;
using TMS.Application.Models;

namespace TMS.Application.Queries.TransactionClient.GetTransactionsClients;

public class GetTransactionsClientsQuery : IRequest<IEnumerable<TransactionExportDto>>
{
    public List<string> RequestedColumns { get; set; } = [];
    public string? SortBy { get; set; }
    public bool SortAsc { get; set; }
    public DateFilterParameters? StartDate { get; set; }
    public DateFilterParameters? EndDate { get; set; }
    public TimeZoneDetails? TimeZone { get; set; }
}
