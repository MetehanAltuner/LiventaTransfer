using LiventaTransfer.API.Extensions;
using LiventaTransfer.Application.Common;
using LiventaTransfer.Application.DTOs.Job;
using LiventaTransfer.Application.Services;
using LiventaTransfer.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LiventaTransfer.API.Controllers;

/// <summary>Transfer İşleri</summary>
[ApiController]
[Route("api/[controller]")]
[Tags("Transfer İşleri")]
public sealed class JobsController : ControllerBase
{
    private readonly JobService _svc;
    public JobsController(JobService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] PagedQuery query,
        [FromQuery] JobStatus? status,
        [FromQuery] long? customerId,
        [FromQuery] long? driverId,
        [FromQuery] long? locationId,
        [FromQuery] DateOnly? dateFrom,
        [FromQuery] DateOnly? dateTo,
        CancellationToken ct)
    {
        var r = await _svc.GetPagedAsync(query, status, customerId, driverId, locationId, dateFrom, dateTo, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var r = await _svc.GetByIdAsync(id, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateJobRequest request, CancellationToken ct)
    {
        var r = await _svc.CreateAsync(request, User.GetUserId(), ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateJobRequest request, CancellationToken ct)
    {
        var r = await _svc.UpdateAsync(id, request, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var r = await _svc.DeleteAsync(id, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpPatch("{id:long}/status")]
    public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateJobStatusRequest request, CancellationToken ct)
    {
        var r = await _svc.UpdateStatusAsync(id, request, User.GetUserId(), ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>İlgili duraktaki yolcuya transfer bilgisinin gönderildiğini işaretler (yolcu bazında).</summary>
    [HttpPost("{id:long}/stops/{stopId:long}/info-sent")]
    public async Task<IActionResult> MarkStopInfoSent(long id, long stopId, CancellationToken ct)
    {
        var r = await _svc.MarkStopInfoSentAsync(id, stopId, ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Verilen iş listesini tek bir işte birleştirir.</summary>
    /// <remarks>En erken JobDate+JobTime'a sahip iş host olarak seçilir; diğerlerinin durakları host'a taşınır ve diğerleri 'Birleştirildi' olarak işaretlenir.</remarks>
    [HttpPost("merge")]
    public async Task<IActionResult> Merge([FromBody] MergeJobsRequest request, CancellationToken ct)
    {
        var r = await _svc.MergeAsync(request, User.GetUserId(), ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpGet("{id:long}/notes")]
    public async Task<IActionResult> GetNotes(long id, CancellationToken ct)
    {
        var noteSvc = HttpContext.RequestServices.GetRequiredService<JobNoteService>();
        var r = await noteSvc.GetByJobIdAsync(id, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpGet("{id:long}/status-history")]
    public async Task<IActionResult> GetStatusHistory(long id, CancellationToken ct)
    {
        var r = await _svc.GetStatusHistoryAsync(id, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpGet("{id:long}/trip-log")]
    public async Task<IActionResult> GetTripLog(long id, CancellationToken ct)
    {
        var tripSvc = HttpContext.RequestServices.GetRequiredService<TripLogService>();
        var r = await tripSvc.GetByJobIdAsync(id, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpPost("confirmation-table")]
    public async Task<IActionResult> GetConfirmationTable([FromBody] List<long> jobIds, CancellationToken ct)
    {
        var tableSvc = HttpContext.RequestServices.GetRequiredService<ConfirmationTableService>();
        var r = await tableSvc.GenerateHtmlAsync(jobIds, ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Konfirme tablosunu tarayıcıda görüntüler (kopyala-yapıştır için)</summary>
    /// <remarks>GET /api/jobs/confirmation-table/html?ids=9,10,11 şeklinde kullanın</remarks>
    [HttpGet("confirmation-table/html")]
    public async Task<IActionResult> GetConfirmationTableHtml([FromQuery] string ids, CancellationToken ct)
    {
        var jobIds = (ids ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => long.TryParse(s, out var id) ? id : (long?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();

        var tableSvc = HttpContext.RequestServices.GetRequiredService<ConfirmationTableService>();
        var r = await tableSvc.GenerateHtmlAsync(jobIds, ct);
        if (!r.Success)
            return StatusCode(r.StatusCode, r.Message);

        var page = $@"<!DOCTYPE html>
<html><head><meta charset=""utf-8""><title>Konfirme Tablosu</title>
<style>body{{font-family:Calibri,sans-serif;padding:20px}}
#copyBtn{{margin-bottom:12px;padding:8px 20px;font-size:14px;cursor:pointer;background:#ffc000;border:1px solid #ccc;border-radius:4px}}
#copyBtn:hover{{background:#e6ad00}}
#msg{{margin-left:10px;color:green;font-size:14px}}</style></head>
<body>
<button id=""copyBtn"" onclick=""copyTable()"">📋 Tabloyu Kopyala</button><span id=""msg""></span>
<div id=""tableContainer"">{r.Data}</div>
<script>
function copyTable(){{
  var container=document.getElementById('tableContainer');
  var range=document.createRange();
  range.selectNodeContents(container);
  var sel=window.getSelection();
  sel.removeAllRanges();
  sel.addRange(range);
  document.execCommand('copy');
  document.getElementById('msg').textContent='✓ Kopyalandı! Şimdi mail\'e yapıştırabilirsiniz.';
  setTimeout(function(){{document.getElementById('msg').textContent='';}},3000);
}}
</script></body></html>";

        return Content(page, "text/html");
    }

}
