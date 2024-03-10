using MediatR;

namespace TMS.Application.Queries.TransactionClient.GetAllTransactionsClients;

public class GetAllTransactionsClientsQuery : IRequest<IEnumerable<TransactionClientPartDto>>
{
    public string? RequestedFields { get; set; }
}
