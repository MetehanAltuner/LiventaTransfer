using FluentValidation;
using LiventaTransfer.Application.DTOs.TripLog;

namespace LiventaTransfer.Application.Validators;

public sealed class CreateTripLogRequestValidator : AbstractValidator<CreateTripLogRequest>
{
    public CreateTripLogRequestValidator()
    {
        RuleFor(x => x.DriverId).NotEmpty().WithMessage("Şoför seçimi zorunludur.");
        RuleFor(x => x.StartKm).GreaterThanOrEqualTo(0).When(x => x.StartKm.HasValue);
        RuleFor(x => x.EndKm).GreaterThanOrEqualTo(0).When(x => x.EndKm.HasValue);
        RuleFor(x => x.WaitingMinutes).GreaterThanOrEqualTo(0).When(x => x.WaitingMinutes.HasValue);
        RuleFor(x => x.FlightStatus).MaximumLength(100);
        RuleFor(x => x.DriverNotes).MaximumLength(2000);
    }
}

public sealed class UpdateTripLogRequestValidator : AbstractValidator<UpdateTripLogRequest>
{
    public UpdateTripLogRequestValidator()
    {
        RuleFor(x => x.DriverId).NotEmpty().WithMessage("Şoför seçimi zorunludur.");
        RuleFor(x => x.StartKm).GreaterThanOrEqualTo(0).When(x => x.StartKm.HasValue);
        RuleFor(x => x.EndKm).GreaterThanOrEqualTo(0).When(x => x.EndKm.HasValue);
        RuleFor(x => x.WaitingMinutes).GreaterThanOrEqualTo(0).When(x => x.WaitingMinutes.HasValue);
        RuleFor(x => x.FlightStatus).MaximumLength(100);
        RuleFor(x => x.DriverNotes).MaximumLength(2000);
    }
}
