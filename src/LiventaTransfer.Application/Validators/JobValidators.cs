using FluentValidation;
using LiventaTransfer.Application.DTOs.Job;

namespace LiventaTransfer.Application.Validators;

public sealed class JobStopRequestValidator : AbstractValidator<JobStopRequest>
{
    public JobStopRequestValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("Müşteri seçimi zorunludur.");
        RuleFor(x => x.Sequence)
            .GreaterThanOrEqualTo(1)
            .When(x => x.Sequence.HasValue)
            .WithMessage("Sıra numarası (sequence) 1 veya daha büyük olmalıdır.");
        RuleFor(x => x.PassengerIds)
            .Must(ids => ids == null || ids.Distinct().Count() == ids.Count)
            .WithMessage("Aynı yolcu bir durağa birden fazla eklenemez.");
        RuleFor(x => x.PickupAddress).MaximumLength(500);
        RuleFor(x => x.DropoffAddress).MaximumLength(500);
        RuleFor(x => x.FlightCode).MaximumLength(20);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.SalePrice).GreaterThanOrEqualTo(0).When(x => x.SalePrice.HasValue);
    }

    /// <summary>Sequence gönderilen durakların sıra numaraları aynı istekte tekrar edemez.</summary>
    internal static bool HaveDistinctSequences(List<JobStopRequest> stops)
    {
        var sequences = (stops ?? [])
            .Where(s => s.Sequence.HasValue)
            .Select(s => s.Sequence!.Value)
            .ToList();
        return sequences.Distinct().Count() == sequences.Count;
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
        RuleFor(x => x.Stops)
            .Must(JobStopRequestValidator.HaveDistinctSequences)
            .WithMessage("Aynı sıra numarası (sequence) birden fazla durakta kullanılamaz.");
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
        RuleFor(x => x.Stops)
            .Must(JobStopRequestValidator.HaveDistinctSequences)
            .WithMessage("Aynı sıra numarası (sequence) birden fazla durakta kullanılamaz.");
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
        RuleFor(x => x.JobIds).NotEmpty().WithMessage("Birleştirilecek en az iki iş seçilmelidir.");
        RuleFor(x => x.JobIds).Must(ids => ids != null && ids.Distinct().Count() >= 2)
            .WithMessage("Birleştirme için en az iki farklı iş gereklidir.");
    }
}

public sealed class JobStopSequenceItemValidator : AbstractValidator<JobStopSequenceItem>
{
    public JobStopSequenceItemValidator()
    {
        RuleFor(x => x.StopId).GreaterThan(0).WithMessage("Geçerli bir durak (stopId) gönderilmelidir.");
        RuleFor(x => x.Sequence).GreaterThanOrEqualTo(1).WithMessage("Sıra numarası (sequence) 1 veya daha büyük olmalıdır.");
    }
}

public sealed class ReorderJobStopsRequestValidator : AbstractValidator<ReorderJobStopsRequest>
{
    public ReorderJobStopsRequestValidator()
    {
        RuleFor(x => x.Stops).NotEmpty().WithMessage("En az bir durak (stop) gereklidir.");
        RuleFor(x => x.Stops)
            .Must(stops => stops == null || stops.Select(s => s.StopId).Distinct().Count() == stops.Count)
            .WithMessage("Aynı durak (stopId) birden fazla gönderilemez.");
        RuleFor(x => x.Stops)
            .Must(stops => stops == null || stops.Select(s => s.Sequence).Distinct().Count() == stops.Count)
            .WithMessage("Aynı sıra numarası (sequence) birden fazla durakta kullanılamaz.");
        RuleForEach(x => x.Stops).SetValidator(new JobStopSequenceItemValidator());
    }
}
