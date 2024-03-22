using MediatR;
using TMS.Application.Models;

namespace TMS.Application.Queries.TransactionClient.GetTransactionsClientsByTimePeriod;

public class GetTransactionsClientsByTimePeriodQuery : IRequest<IEnumerable<TransactionDto>>
{
    public DateTimeOffset DateFrom { get; set; } = DateTimeOffset.MinValue;
    public DateTimeOffset DateTo { get; set; } = DateTimeOffset.MaxValue;
}
