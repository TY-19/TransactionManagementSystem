using FluentValidation;

namespace TMS.Application.Commands.Client.AddUpdateClient;

public class AddUpdateClientCommandValidator : AbstractValidator<AddUpdateClientCommand>
{
    public AddUpdateClientCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty();
        RuleFor(c => c.Email)
            .NotEmpty()
            .EmailAddress();
        RuleFor(c => c.Latitude)
            .GreaterThanOrEqualTo(-90m)
                .WithMessage("Latitude cannot be less than -90 degrees.")
            .LessThanOrEqualTo(90m)
                .WithMessage("Latitude cannot be more than 90 degrees.");
        RuleFor(c => c.Longitude)
            .GreaterThanOrEqualTo(-180m)
                .WithMessage("Longitude cannot be less than -180 degrees.")
            .LessThanOrEqualTo(180m)
                .WithMessage("Longitude cannot be more than 180 degrees.");
    }
}
