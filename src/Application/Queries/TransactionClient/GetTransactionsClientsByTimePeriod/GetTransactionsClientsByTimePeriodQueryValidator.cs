using FluentValidation;

namespace TMS.Application.Queries.TransactionClient.GetTransactionsClientsByTimePeriod;

public class GetTransactionsClientsByTimePeriodQueryValidator
    : AbstractValidator<GetTransactionsClientsByTimePeriodQuery>
{
    public GetTransactionsClientsByTimePeriodQueryValidator()
    {
        RuleFor(tc => tc.DateFrom)
            .NotNull()
            .GreaterThanOrEqualTo(DateOnly.MinValue)
            .LessThanOrEqualTo(tc => tc.DateTo);
        RuleFor(tc => tc.DateTo)
            .NotNull()
            .GreaterThanOrEqualTo(tc => tc.DateFrom)
            .LessThanOrEqualTo(DateOnly.MaxValue);
    }
}
