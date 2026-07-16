using LiventaTransfer.Application.Common;
using LiventaTransfer.Application.DTOs.Job;

namespace LiventaTransfer.Tests;

public class JobStopSequenceHelperTests
{
    private static JobStopRequest Stop(string note, int? sequence = null) =>
        new() { CustomerId = 1, Notes = note, Sequence = sequence };

    [Fact]
    public void HicSequenceGonderilmemisse_ListeSirasiKorunur()
    {
        var stops = new List<JobStopRequest> { Stop("a"), Stop("b"), Stop("c") };

        var result = JobStopSequenceHelper.OrderByRequestedSequence(stops);

        Assert.Equal(["a", "b", "c"], result.Select(s => s.Notes));
    }

    [Fact]
    public void SequenceGonderilmisse_DuraklarSequenceGoreSiralanir()
    {
        var stops = new List<JobStopRequest>
        {
            Stop("a", 3),
            Stop("b", 1),
            Stop("c", 2)
        };

        var result = JobStopSequenceHelper.OrderByRequestedSequence(stops);

        Assert.Equal(["b", "c", "a"], result.Select(s => s.Notes));
    }

    [Fact]
    public void SequenceOlmayanDuraklar_ListeSirasiKorunarakSonaEklenir()
    {
        var stops = new List<JobStopRequest>
        {
            Stop("a"),
            Stop("b", 2),
            Stop("c"),
            Stop("d", 1)
        };

        var result = JobStopSequenceHelper.OrderByRequestedSequence(stops);

        Assert.Equal(["d", "b", "a", "c"], result.Select(s => s.Notes));
    }

    [Fact]
    public void SequenceDegerleriArtikOlabilir_SiralamaGoreceliYapilir()
    {
        // 1..n olmayan (aralikli) degerler de goreceli siralama icin kullanilabilir
        var stops = new List<JobStopRequest>
        {
            Stop("a", 100),
            Stop("b", 5),
            Stop("c", 42)
        };

        var result = JobStopSequenceHelper.OrderByRequestedSequence(stops);

        Assert.Equal(["b", "c", "a"], result.Select(s => s.Notes));
    }

    [Fact]
    public void AyniSequenceDegerinde_ListeSirasiTieBreakerOlur()
    {
        var stops = new List<JobStopRequest>
        {
            Stop("a", 1),
            Stop("b", 1),
            Stop("c", 1)
        };

        var result = JobStopSequenceHelper.OrderByRequestedSequence(stops);

        Assert.Equal(["a", "b", "c"], result.Select(s => s.Notes));
    }
}
