using LiventaTransfer.Application.DTOs.Job;
using LiventaTransfer.Application.Validators;
using LiventaTransfer.Domain.Enums;

namespace LiventaTransfer.Tests;

public class JobValidatorSequenceTests
{
    private static JobStopRequest Stop(int? sequence = null) =>
        new() { CustomerId = 1, Sequence = sequence };

    [Fact]
    public void JobStopRequest_SequenceSifirVeyaNegatif_Gecersiz()
    {
        var validator = new JobStopRequestValidator();

        Assert.False(validator.Validate(Stop(0)).IsValid);
        Assert.False(validator.Validate(Stop(-1)).IsValid);
    }

    [Fact]
    public void JobStopRequest_SequenceBirVeyaBuyuk_Gecerli()
    {
        var validator = new JobStopRequestValidator();

        Assert.True(validator.Validate(Stop(1)).IsValid);
        Assert.True(validator.Validate(Stop(10)).IsValid);
    }

    [Fact]
    public void JobStopRequest_SequenceGonderilmemisse_Gecerli()
    {
        var validator = new JobStopRequestValidator();

        Assert.True(validator.Validate(Stop()).IsValid);
    }

    [Fact]
    public void CreateJobRequest_DuplicateSequence_Gecersiz()
    {
        var validator = new CreateJobRequestValidator();
        var request = new CreateJobRequest { JobType = JobType.Transfer, Stops = [Stop(1), Stop(1)] };

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            e => e.ErrorMessage == "Aynı sıra numarası (sequence) birden fazla durakta kullanılamaz.");
    }

    [Fact]
    public void CreateJobRequest_FarkliSequenceVeSequencesizKarisik_Gecerli()
    {
        var validator = new CreateJobRequestValidator();
        var request = new CreateJobRequest { JobType = JobType.Transfer, Stops = [Stop(2), Stop(1), Stop()] };

        Assert.True(validator.Validate(request).IsValid);
    }

    [Fact]
    public void UpdateJobRequest_DuplicateSequence_Gecersiz()
    {
        var validator = new UpdateJobRequestValidator();
        var request = new UpdateJobRequest { JobType = JobType.Transfer, Stops = [Stop(3), Stop(3)] };

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            e => e.ErrorMessage == "Aynı sıra numarası (sequence) birden fazla durakta kullanılamaz.");
    }

    [Fact]
    public void ReorderRequest_BosListe_Gecersiz()
    {
        var validator = new ReorderJobStopsRequestValidator();

        Assert.False(validator.Validate(new ReorderJobStopsRequest()).IsValid);
    }

    [Fact]
    public void ReorderRequest_DuplicateStopId_Gecersiz()
    {
        var validator = new ReorderJobStopsRequestValidator();
        var request = new ReorderJobStopsRequest
        {
            Stops =
            [
                new JobStopSequenceItem { StopId = 5, Sequence = 1 },
                new JobStopSequenceItem { StopId = 5, Sequence = 2 }
            ]
        };

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            e => e.ErrorMessage == "Aynı durak (stopId) birden fazla gönderilemez.");
    }

    [Fact]
    public void ReorderRequest_DuplicateSequence_Gecersiz()
    {
        var validator = new ReorderJobStopsRequestValidator();
        var request = new ReorderJobStopsRequest
        {
            Stops =
            [
                new JobStopSequenceItem { StopId = 5, Sequence = 1 },
                new JobStopSequenceItem { StopId = 6, Sequence = 1 }
            ]
        };

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            e => e.ErrorMessage == "Aynı sıra numarası (sequence) birden fazla durakta kullanılamaz.");
    }

    [Fact]
    public void ReorderRequest_SequenceSifir_Gecersiz()
    {
        var validator = new ReorderJobStopsRequestValidator();
        var request = new ReorderJobStopsRequest
        {
            Stops = [new JobStopSequenceItem { StopId = 5, Sequence = 0 }]
        };

        Assert.False(validator.Validate(request).IsValid);
    }

    [Fact]
    public void ReorderRequest_Gecerliistek_Gecer()
    {
        var validator = new ReorderJobStopsRequestValidator();
        var request = new ReorderJobStopsRequest
        {
            Stops =
            [
                new JobStopSequenceItem { StopId = 5, Sequence = 2 },
                new JobStopSequenceItem { StopId = 6, Sequence = 1 }
            ]
        };

        Assert.True(validator.Validate(request).IsValid);
    }
}
