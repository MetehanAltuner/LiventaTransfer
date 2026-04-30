using FluentValidation;
using LiventaTransfer.Application.DTOs.Job;

namespace LiventaTransfer.Application.Validators;

public sealed class JobStopRequestValidator : AbstractValidator<JobStopRequest>
{
    public JobStopRequestValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("Müşteri seçimi zorunludur.");
        RuleFor(x => x.PassengerCount).GreaterThan(0).WithMessage("Yolcu sayısı en az 1 olmalıdır.");
        RuleFor(x => x.PickupAddress).MaximumLength(500);
        RuleFor(x => x.DropoffAddress).MaximumLength(500);
        RuleFor(x => x.FlightCode).MaximumLength(20);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.SalePrice).GreaterThanOrEqualTo(0).When(x => x.SalePrice.HasValue);
    }
}

public sealed class CreateJobRequestValidator : AbstractValidator<CreateJobRequest>
{
    public CreateJobRequestValidator()
    {
        RuleFor(x => x.JobType).IsInEnum();
        RuleFor(x => x.RouteDescription).MaximumLength(1000);
        RuleFor(x => x.ExtraInfo).MaximumLength(2000);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.SourceEmail).MaximumLength(500);
        RuleFor(x => x.PurchasePrice).GreaterThanOrEqualTo(0).When(x => x.PurchasePrice.HasValue);
        RuleFor(x => x.ExtraCost).GreaterThanOrEqualTo(0).When(x => x.ExtraCost.HasValue);
        RuleFor(x => x.Stops).NotEmpty().WithMessage("En az bir durak (stop) gereklidir.");
        RuleForEach(x => x.Stops).SetValidator(new JobStopRequestValidator());
    }
}

public sealed class UpdateJobRequestValidator : AbstractValidator<UpdateJobRequest>
{
    public UpdateJobRequestValidator()
    {
        RuleFor(x => x.JobType).IsInEnum();
        RuleFor(x => x.RouteDescription).MaximumLength(1000);
        RuleFor(x => x.ExtraInfo).MaximumLength(2000);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.SourceEmail).MaximumLength(500);
        RuleFor(x => x.PurchasePrice).GreaterThanOrEqualTo(0).When(x => x.PurchasePrice.HasValue);
        RuleFor(x => x.ExtraCost).GreaterThanOrEqualTo(0).When(x => x.ExtraCost.HasValue);
        RuleFor(x => x.Stops).NotEmpty().WithMessage("En az bir durak (stop) gereklidir.");
        RuleForEach(x => x.Stops).SetValidator(new JobStopRequestValidator());
    }
}

public sealed class UpdateJobStatusRequestValidator : AbstractValidator<UpdateJobStatusRequest>
{
    public UpdateJobStatusRequestValidator()
    {
        RuleFor(x => x.NewStatus).IsInEnum().WithMessage("Geçerli bir durum seçiniz.");
        RuleFor(x => x.ChangeReason).MaximumLength(500);
    }
}

public sealed class MergeJobsRequestValidator : AbstractValidator<MergeJobsRequest>
{
    public MergeJobsRequestValidator()
    {
        RuleFor(x => x.SourceJobIds).NotEmpty().WithMessage("En az bir kaynak iş seçilmelidir.");
    }
}
