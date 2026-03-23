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
            .Include(j => j.Passenger)
            .Include(j => j.PickupLocation)
            .Include(j => j.DropoffLocation)
            .Where(j => jobIds.Contains(j.Id))
            .OrderBy(j => j.JobDate).ThenBy(j => j.JobTime)
            .ToListAsync(ct);

        if (jobs.Count == 0)
            return ApiResult<string>.Fail("Belirtilen ID'lere ait iş bulunamadı.", statusCode: 404);

        var sb = new StringBuilder();

        sb.Append(@"<table border=""0"" cellspacing=""0"" cellpadding=""0"" style=""border-collapse:collapse;font-family:Calibri,sans-serif"">");

        // Header row - sarı arka plan (#ffc000)
        sb.Append(@"<tr style=""height:18pt"">");
        AppendHeaderCell(sb, "Tarih", "62pt");
        AppendHeaderCell(sb, "saat", "40pt");
        AppendHeaderCell(sb, "Yolcu İsmi", "184pt");
        AppendHeaderCell(sb, "Alış Yeri", "127pt");
        AppendHeaderCell(sb, "Bırakış Yeri", "121pt");
        AppendHeaderCell(sb, "Tek Yön Ücreti", "85pt");
        sb.Append("</tr>");

        // Data rows
        foreach (var job in jobs)
        {
            var passengerName = job.Passenger?.FullName ?? "-";
            var pickup = job.PickupLocation?.Name ?? job.PickupAddress ?? "-";
            var dropoff = job.DropoffLocation?.Name ?? job.DropoffAddress ?? "-";
            var price = job.SalePrice.HasValue
                ? $"₺{job.SalePrice.Value.ToString("N2", new CultureInfo("tr-TR"))}"
                : "-";

            sb.Append(@"<tr style=""height:20pt"">");
            AppendDataCell(sb, job.JobDate.ToString("dd.MM.yyyy"), "62pt", "center");
            AppendDataCell(sb, job.JobTime.ToString("HH:mm"), "40pt", "center");
            AppendDataCell(sb, passengerName, "184pt", "center");
            AppendDataCell(sb, pickup, "127pt", "center");
            AppendDataCell(sb, dropoff, "121pt", "center");
            AppendDataCell(sb, price, "85pt", "right");
            sb.Append("</tr>");
        }

        sb.Append("</table>");

        return ApiResult<string>.Ok(sb.ToString(), "Konfirme tablosu oluşturuldu.");
    }

    private static void AppendHeaderCell(StringBuilder sb, string text, string width)
    {
        sb.Append($@"<td style=""width:{width};border:solid #000 1pt;background:#ffc000;padding:0 3.5pt;text-align:center""><b><span style=""font-size:11pt;color:black"">{text}</span></b></td>");
    }

    private static void AppendDataCell(StringBuilder sb, string text, string width, string align)
    {
        sb.Append($@"<td style=""width:{width};border:solid #000 1pt;border-top:none;background:white;padding:0 3.5pt;text-align:{align}""><span style=""font-size:10pt;font-family:Arial,sans-serif;color:black"">{text}</span></td>");
    }
}
