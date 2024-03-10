using MediatR;
using TMS.Application.Common;
using TMS.Application.Models.Dtos;

namespace TMS.Application.Queries.TransactionClient.GetAllTransactionsClients;

public class GetAllTransactionsClientsQuery : IRequest<IEnumerable<TransactionClientExportDto>>
{
    public List<PropertyNames> RequestedColumns { get; set; } = [];
}
