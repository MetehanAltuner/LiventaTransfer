using FluentValidation;
using LiventaTransfer.Application.DTOs.Driver;

namespace LiventaTransfer.Application.Validators;

public sealed class CreateDriverRequestValidator : AbstractValidator<CreateDriverRequest>
{
    public CreateDriverRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20).WithMessage("Telefon zorunludur.");
        RuleFor(x => x.LicenseNumber).MaximumLength(50);
        RuleFor(x => x.VehicleOwnerId).NotEmpty().WithMessage("Araç sahibi zorunludur.");
    }
}

public sealed class UpdateDriverRequestValidator : AbstractValidator<UpdateDriverRequest>
{
    public UpdateDriverRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20).WithMessage("Telefon zorunludur.");
        RuleFor(x => x.LicenseNumber).MaximumLength(50);
        RuleFor(x => x.VehicleOwnerId).NotEmpty().WithMessage("Araç sahibi zorunludur.");
    }
}
