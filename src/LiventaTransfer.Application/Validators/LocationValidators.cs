using FluentValidation;
using LiventaTransfer.Application.DTOs.Location;

namespace LiventaTransfer.Application.Validators;

public sealed class CreateLocationRequestValidator : AbstractValidator<CreateLocationRequest>
{
    public CreateLocationRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ShortCode).MaximumLength(20);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.LocationType).IsInEnum().WithMessage("Geçersiz lokasyon tipi.");
    }
}

public sealed class UpdateLocationRequestValidator : AbstractValidator<UpdateLocationRequest>
{
    public UpdateLocationRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ShortCode).MaximumLength(20);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.LocationType).IsInEnum().WithMessage("Geçersiz lokasyon tipi.");
    }
}
