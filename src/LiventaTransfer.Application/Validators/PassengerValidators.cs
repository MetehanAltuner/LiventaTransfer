using FluentValidation;
using LiventaTransfer.Application.DTOs.Passenger;

namespace LiventaTransfer.Application.Validators;

public sealed class CreatePassengerRequestValidator : AbstractValidator<CreatePassengerRequest>
{
    public CreatePassengerRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NationalId)
            .Matches(@"^\d{11}$").WithMessage("TC kimlik numarası 11 haneli olmalıdır.")
            .When(x => !string.IsNullOrWhiteSpace(x.NationalId));
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Email).MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public sealed class UpdatePassengerRequestValidator : AbstractValidator<UpdatePassengerRequest>
{
    public UpdatePassengerRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NationalId)
            .Matches(@"^\d{11}$").WithMessage("TC kimlik numarası 11 haneli olmalıdır.")
            .When(x => !string.IsNullOrWhiteSpace(x.NationalId));
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Email).MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
