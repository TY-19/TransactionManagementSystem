using FluentValidation;

namespace TMS.Application.Commands.Transaction.AddUpdateTransaction;

public class AddUpdateTransactionCommandValidator : AbstractValidator<AddUpdateTransactionCommand>
{
    public AddUpdateTransactionCommandValidator()
    {
        RuleFor(t => t.TransactionId).NotEmpty();
        RuleFor(t => t.ClientEmail).NotEmpty().EmailAddress();
        RuleFor(t => t.Amount).NotNull();
        RuleFor(t => t.TransactionDate).NotNull()
            .Must(td => DateTimeOffset.UtcNow > td);
    }
}
