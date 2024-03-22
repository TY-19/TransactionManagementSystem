using FluentValidation;

namespace TMS.Application.Queries.TransactionClient.GetTransactionsClientsByTimePeriod;

public class GetTransactionsClientsByTimePeriodQueryValidator
    : AbstractValidator<GetTransactionsClientsByTimePeriodQuery>
{
    public GetTransactionsClientsByTimePeriodQueryValidator()
    {
        RuleFor(tc => tc.DateFrom)
            .NotNull()
            .GreaterThanOrEqualTo(DateTimeOffset.MinValue)
            .LessThanOrEqualTo(tc => tc.DateTo);
        RuleFor(tc => tc.DateTo)
            .NotNull()
            .GreaterThanOrEqualTo(tc => tc.DateFrom)
            .LessThanOrEqualTo(DateTimeOffset.MaxValue);
    }
}
