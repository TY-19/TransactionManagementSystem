using FluentValidation;
using TMS.Application.Interfaces;
using TMS.Application.Models;

namespace TMS.Application.Queries.TransactionClient.GetTransactionsClients;

public class GetTransactionsClientsQueryValidator : AbstractValidator<GetTransactionsClientsQuery>
{
    public GetTransactionsClientsQueryValidator(ITransactionPropertyHelper propertyManager)
    {
        RuleFor(tc => tc.RequestedColumns)
            .NotEmpty().WithMessage("At least one column must be requested");
        RuleForEach(tc => tc.RequestedColumns)
            .Must(propertyManager.IsDatabaseColumnName).WithMessage("Invalid column name");
        RuleFor(tc => tc.SortBy)
            .Must(sb => sb == null || propertyManager.IsDatabaseColumnName(sb)).WithMessage("Invalid column name");
        RuleFor(tc => tc.StartDate)
            .Must(ValidateDateFilterParameters).WithMessage("Start date is not a valid date.");
        RuleFor(tc => tc.EndDate)
            .Must(ValidateDateFilterParameters).WithMessage("End date is not a valid date.");
    }

    private bool ValidateDateFilterParameters(DateFilterParameters? dateFilter)
    {
        if (dateFilter == null)
            return true;

        if (dateFilter.Year < 0 || dateFilter.Year > 3000
            || dateFilter.Month < 1 || dateFilter.Month > 12
            || dateFilter.Day < 1
            || dateFilter.Day > DateTime.DaysInMonth(dateFilter.Year, dateFilter.Month))
        {
            return false;
        }
        return true;
    }
}
