using FluentValidation;
using LiventaTransfer.Application.DTOs.VehicleOwner;

namespace LiventaTransfer.Application.Validators;

public sealed class CreateVehicleOwnerRequestValidator : AbstractValidator<CreateVehicleOwnerRequest>
{
    public CreateVehicleOwnerRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactPerson).MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Email).MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public sealed class UpdateVehicleOwnerRequestValidator : AbstractValidator<UpdateVehicleOwnerRequest>
{
    public UpdateVehicleOwnerRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactPerson).MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Email).MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
