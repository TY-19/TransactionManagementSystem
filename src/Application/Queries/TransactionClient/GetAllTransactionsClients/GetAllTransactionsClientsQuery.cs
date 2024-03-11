using MediatR;
using TMS.Application.Models.Dtos;

namespace TMS.Application.Queries.TransactionClient.GetAllTransactionsClients;

public class GetAllTransactionsClientsQuery : IRequest<IEnumerable<TransactionClientExportDto>>
{
    public List<string> RequestedColumns { get; set; } = [];
}
