using System.Globalization;
using System.Text;
using LiventaTransfer.Application.Common;
using LiventaTransfer.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LiventaTransfer.Application.Services;

public sealed class ConfirmationTableService
{
    private readonly IAppDbContext _db;
    public ConfirmationTableService(IAppDbContext db) => _db = db;

    public async Task<ApiResult<string>> GenerateHtmlAsync(List<long> jobIds, CancellationToken ct)
    {
        if (jobIds.Count == 0)
            return ApiResult<string>.Fail("En az bir iş ID'si gereklidir.", statusCode: 400);

        var jobs = await _db.Jobs.AsNoTracking()
            .Include(j => j.Stops).ThenInclude(s => s.Passengers).ThenInclude(p => p.Passenger)
            .Include(j => j.Stops).ThenInclude(s => s.PickupLocation)
            .Include(j => j.Stops).ThenInclude(s => s.DropoffLocation)
            .Where(j => jobIds.Contains(j.Id))
            .OrderBy(j => j.JobDate).ThenBy(j => j.JobTime)
            .ToListAsync(ct);

        if (jobs.Count == 0)
            return ApiResult<string>.Fail("Belirtilen ID'lere ait iş bulunamadı.", statusCode: 404);

        var sb = new StringBuilder();

        // Outlook uyumlu tablo formatı (EML konfirme maili ile birebir aynı)
        sb.Append(@"<table border=""0"" cellspacing=""0"" cellpadding=""0"" width=""825"" style=""width:619.0pt;border-collapse:collapse"">");

        // Header row - sarı arka plan (#ffc000)
        sb.Append(@"<tr style=""height:18.0pt"">");
        AppendHeaderCell(sb, "Tarih", 83, "62.0pt", isFirst: true);
        AppendHeaderCell(sb, "saat", 53, "40.0pt");
        AppendHeaderCell(sb, "Yolcu İsmi", 245, "184.0pt");
        AppendHeaderCell(sb, "Alış Yeri", 169, "127.0pt");
        AppendHeaderCell(sb, "Bırakış Yeri", 161, "121.0pt");
        AppendHeaderCell(sb, "Tek Yön Ücreti", 113, "85.0pt");
        sb.Append("</tr>");

        // Data rows — bir Job'da birden fazla durak varsa her durak ayrı satır olur
        foreach (var job in jobs)
        {
            foreach (var stop in job.Stops.OrderBy(s => s.Sequence))
            {
                var passengerName = stop.Passengers.Count > 0
                    ? string.Join(", ", stop.Passengers
                        .Where(p => p.Passenger != null)
                        .Select(p => p.Passenger!.FullName))
                    : "-";
                if (string.IsNullOrWhiteSpace(passengerName)) passengerName = "-";
                var pickup = stop.PickupLocation?.Name ?? stop.PickupAddress ?? "-";
                var dropoff = stop.DropoffLocation?.Name ?? stop.DropoffAddress ?? "-";
                var price = stop.SalePrice.HasValue
                    ? $"₺{stop.SalePrice.Value.ToString("N2", new CultureInfo("tr-TR"))}"
                    : "-";

                sb.Append(@"<tr style=""height:19.95pt"">");
                AppendDataCell(sb, job.JobDate.ToString("dd.MM.yyyy"), 83, "62.0pt", "center", isFirst: true);
                AppendDataCell(sb, job.JobTime.ToString("HH:mm"), 53, "40.0pt", "center");
                AppendDataCell(sb, passengerName, 245, "184.0pt", "center");
                AppendDataCell(sb, pickup, 169, "127.0pt", "center");
                AppendDataCell(sb, dropoff, 161, "121.0pt", "center");
                AppendDataCell(sb, price, 113, "85.0pt", "right");
                sb.Append("</tr>");
            }
        }

        sb.Append("</tbody></table>");

        return ApiResult<string>.Ok(sb.ToString(), "Konfirme tablosu oluşturuldu.");
    }

    private static void AppendHeaderCell(StringBuilder sb, string text, int widthPx, string widthPt, bool isFirst = false)
    {
        var border = isFirst
            ? "border:solid windowtext 1.0pt"
            : "border:solid windowtext 1.0pt;border-left:none";
        sb.Append($@"<td width=""{widthPx}"" nowrap style=""width:{widthPt};{border};background:#ffc000;padding:0cm 3.5pt 0cm 3.5pt;height:18.0pt""><p class=""MsoNormal"" align=""center"" style=""text-align:center""><b><span style=""font-size:11.0pt;color:black"">{text}</span></b></p></td>");
    }

    private static void AppendDataCell(StringBuilder sb, string text, int widthPx, string widthPt, string align, bool isFirst = false)
    {
        var border = isFirst
            ? "border:solid windowtext 1.0pt;border-top:none"
            : "border-top:none;border-left:none;border-bottom:solid windowtext 1.0pt;border-right:solid windowtext 1.0pt";
        sb.Append($@"<td width=""{widthPx}"" nowrap style=""width:{widthPt};{border};background:white;padding:0cm 3.5pt 0cm 3.5pt;height:19.95pt""><p class=""MsoNormal"" align=""{align}"" style=""text-align:{align}""><span style=""font-size:10.0pt;font-family:&quot;Arial&quot;,sans-serif;color:black"">{text}</span></p></td>");
    }
}
