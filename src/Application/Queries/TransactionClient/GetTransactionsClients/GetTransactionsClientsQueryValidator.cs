using FluentValidation;

namespace TMS.Application.Queries.TransactionClient.GetTransactionsClients;

public class GetTransactionsClientsQueryValidator : AbstractValidator<GetTransactionsClientsQuery>
{
    public GetTransactionsClientsQueryValidator()
    {
        RuleFor(tc => tc.RequestedColumns).NotNull()
            .NotEmpty().WithMessage("At least one column must be requested");
    }
}
