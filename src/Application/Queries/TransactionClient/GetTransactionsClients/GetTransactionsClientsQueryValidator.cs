using FluentValidation;
using System.Text.RegularExpressions;
using TMS.Application.Interfaces;

namespace TMS.Application.Queries.TransactionClient.GetTransactionsClients;

public partial class GetTransactionsClientsQueryValidator : AbstractValidator<GetTransactionsClientsQuery>
{
    // In some cases (e.g. 'Pacific/Kiritimati' time zone) offset can be up to 14:00.
    private const int MaxOffsetFromUtcHours = 14;
    private const int MinOffsetFromUtcHours = -12;
    public GetTransactionsClientsQueryValidator(ITransactionPropertyHelper propertyManager)
    {
        RuleFor(tc => tc.ColumnNames)
            .NotEmpty()
                .WithMessage("At least one column must be requested.");
        RuleForEach(tc => tc.ColumnNames)
            .Must(propertyManager.IsDatabaseColumnName)
                .WithMessage("Invalid column name.");
        RuleFor(tc => tc.SortBy)
            .Must(sb => sb == null || propertyManager.IsDatabaseColumnName(sb))
                .WithMessage("Invalid column name.");
        RuleFor(tc => tc.StartDate)
            .Must((tc, sd) => sd == null || !tc.EndDate.HasValue || sd < tc.EndDate.Value)
                .WithMessage("Start date must be lesser than end date.");
        RuleFor(tc => tc.EndDate)
            .Must((tc, ed) => ed == null || !tc.StartDate.HasValue || ed < tc.StartDate.Value)
                .WithMessage("End date must be greater than start date.");
        RuleFor(tc => tc.StartDateOffset)
            .NotEmpty()
                .WithMessage("Start offset must be provided.")
            .Must(ValidateReadableOffset)
                .WithMessage("Start date offset must be in the format '+00:00'.");
        RuleFor(tc => tc.EndDateOffset)
            .NotEmpty()
                .WithMessage("End offset must be provided.")
            .Must(ValidateReadableOffset)
                .WithMessage("End date offset must be in the format '+00:00'.");
    }

    [GeneratedRegex("^[+-]{1}(\\d{2}):(\\d{2})$")]
    private static partial Regex OffsetRegex();
    private bool ValidateReadableOffset(string offset)
    {
        Match match = OffsetRegex().Match(offset);
        if (match.Success
            && int.TryParse(match.Groups[1].Value, out int hours)
            && int.TryParse(match.Groups[2].Value, out int minutes))
        {
            return hours >= MinOffsetFromUtcHours && hours <= MaxOffsetFromUtcHours
                && minutes >= 0 && minutes <= 60;
        }
        return false;
    }
}
