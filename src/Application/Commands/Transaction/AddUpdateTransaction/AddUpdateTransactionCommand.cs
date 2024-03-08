using MediatR;
using TMS.Application.Models;

namespace TMS.Application.Commands.Transaction.AddUpdateTransaction;

public class AddUpdateTransactionCommand : IRequest
{
    public string TransactionId { get; set; } = null!;
    public string ClientEmail { get; set; } = null!;
    public decimal Amount { get; set; }
    public DateTimeOffset TransactionDate { get; set; }
}
