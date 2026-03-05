using FluentValidation;
using LiventaTransfer.Application.DTOs.Vehicle;

namespace LiventaTransfer.Application.Validators;

public sealed class CreateVehicleRequestValidator : AbstractValidator<CreateVehicleRequest>
{
    public CreateVehicleRequestValidator()
    {
        RuleFor(x => x.Plate).NotEmpty().MaximumLength(20).WithMessage("Plaka zorunludur.");
        RuleFor(x => x.VehicleType).IsInEnum().WithMessage("Geçersiz araç tipi.");
        RuleFor(x => x.Brand).MaximumLength(100);
        RuleFor(x => x.Model).MaximumLength(100);
        RuleFor(x => x.Capacity).GreaterThan(0).WithMessage("Kapasite 0'dan büyük olmalıdır.");
        RuleFor(x => x.VehicleOwnerId).NotEmpty().WithMessage("Araç sahibi zorunludur.");
    }
}

public sealed class UpdateVehicleRequestValidator : AbstractValidator<UpdateVehicleRequest>
{
    public UpdateVehicleRequestValidator()
    {
        RuleFor(x => x.Plate).NotEmpty().MaximumLength(20).WithMessage("Plaka zorunludur.");
        RuleFor(x => x.VehicleType).IsInEnum().WithMessage("Geçersiz araç tipi.");
        RuleFor(x => x.Brand).MaximumLength(100);
        RuleFor(x => x.Model).MaximumLength(100);
        RuleFor(x => x.Capacity).GreaterThan(0).WithMessage("Kapasite 0'dan büyük olmalıdır.");
        RuleFor(x => x.VehicleOwnerId).NotEmpty().WithMessage("Araç sahibi zorunludur.");
    }
}
