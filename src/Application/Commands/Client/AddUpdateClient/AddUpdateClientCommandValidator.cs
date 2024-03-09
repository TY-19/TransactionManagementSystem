using FluentValidation;

namespace TMS.Application.Commands.Client.AddUpdateClient;

public class AddUpdateClientCommandValidator : AbstractValidator<AddUpdateClientCommand>
{
    public AddUpdateClientCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty();
        RuleFor(c => c.Email).NotEmpty()
            .EmailAddress();
        RuleFor(c => c.Latitude).NotEmpty()
            .GreaterThanOrEqualTo(-90m)
            .LessThanOrEqualTo(90m);
        RuleFor(c => c.Longitude).NotEmpty()
            .GreaterThanOrEqualTo(-180m)
            .LessThanOrEqualTo(180m);
    }
}
