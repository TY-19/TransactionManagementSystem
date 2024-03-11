using MediatR;
using TMS.Application.Models.Dtos;
using TMS.Domain.Enums;

namespace TMS.Application.Queries.TransactionClient.GetAllTransactionsClients;

public class GetAllTransactionsClientsQuery : IRequest<IEnumerable<TransactionClientExportDto>>
{
    public List<string> RequestedColumns { get; set; } = [];
}
