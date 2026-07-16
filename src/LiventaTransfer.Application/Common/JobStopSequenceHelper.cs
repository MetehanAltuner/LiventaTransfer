using LiventaTransfer.Application.DTOs.Job;

namespace LiventaTransfer.Application.Common;

public static class JobStopSequenceHelper
{
    /// <summary>
    /// İstekteki herhangi bir durakta Sequence gönderilmişse durakları bu değerlere göre sıralar;
    /// Sequence'i olmayan duraklar liste sırası korunarak sona eklenir. Hiçbir durakta Sequence
    /// yoksa liste sırası aynen korunur (varsayılan davranış). Aynı Sequence değeri birden fazla
    /// durakta varsa liste sırası tie-breaker olarak kullanılır. Sıra numaraları, duraklar
    /// oluşturulurken listedeki konumdan 1..n olarak normalize edilir.
    /// </summary>
    public static List<JobStopRequest> OrderByRequestedSequence(List<JobStopRequest> stops)
    {
        if (stops.All(s => !s.Sequence.HasValue))
            return stops;

        var indexed = stops.Select((stop, index) => (stop, index)).ToList();

        return indexed
            .Where(x => x.stop.Sequence.HasValue)
            .OrderBy(x => x.stop.Sequence!.Value)
            .ThenBy(x => x.index)
            .Concat(indexed.Where(x => !x.stop.Sequence.HasValue))
            .Select(x => x.stop)
            .ToList();
    }
}
