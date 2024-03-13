using MediatR;
using TMS.Application.Models;
using TMS.Application.Models.Dtos;

namespace TMS.Application.Queries.TransactionClient.GetTransactionsClients;

public class GetTransactionsClientsQuery : IRequest<IEnumerable<TransactionClientExportDto>>
{
    public List<string> RequestedColumns { get; set; } = [];
    public string SortBy { get; set; } = null!;
    public bool SortAsc { get; set; } = true;
    public DateFilterParameters? StartDate { get; set; }
    public DateFilterParameters? EndDate { get; set; }
    public string? UserTimeZoneOffset { get; set; }
}
