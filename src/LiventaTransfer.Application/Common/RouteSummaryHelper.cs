namespace LiventaTransfer.Application.Common;

public static class RouteSummaryHelper
{
    /// <summary>
    /// İşin güzergahı: önce tüm durakların alış lokasyon isimleri (sıra ile), ardından tüm
    /// durakların varış lokasyon isimleri " -> " ile birleştirilir. Aynı lokasyon adı
    /// (alış/varış fark etmeksizin) yalnızca ilk geçtiği yerde bir kez yazılır.
    /// </summary>
    public static string Build(IEnumerable<Domain.Entities.JobStop> stops)
    {
        var ordered = stops.OrderBy(s => s.Sequence).ToList();

        var names = new List<string>();
        foreach (var s in ordered)
        {
            var name = s.PickupLocation?.Name ?? s.PickupAddress;
            if (!string.IsNullOrWhiteSpace(name)) names.Add(name.Trim());
        }
        foreach (var s in ordered)
        {
            var name = s.DropoffLocation?.Name ?? s.DropoffAddress;
            if (!string.IsNullOrWhiteSpace(name)) names.Add(name.Trim());
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return string.Join(" -> ", names.Where(seen.Add));
    }
}
