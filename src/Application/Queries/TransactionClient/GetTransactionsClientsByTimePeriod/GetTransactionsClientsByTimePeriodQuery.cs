using MediatR;
using TMS.Application.Models;

namespace TMS.Application.Queries.TransactionClient.GetTransactionsClientsByTimePeriod;

public class GetTransactionsClientsByTimePeriodQuery : IRequest<IEnumerable<TransactionDto>>
{
    public DateOnly DateFrom { get; set; } = DateOnly.MinValue;
    public DateOnly DateTo { get; set; } = DateOnly.MaxValue;
}
