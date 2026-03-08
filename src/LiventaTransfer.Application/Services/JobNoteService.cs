using LiventaTransfer.Application.Common;
using LiventaTransfer.Application.DTOs.JobNote;
using LiventaTransfer.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LiventaTransfer.Application.Services;

public sealed class JobNoteService
{
    private readonly IAppDbContext _db;
    public JobNoteService(IAppDbContext db) => _db = db;

    public async Task<ApiResult<List<JobNoteDto>>> GetByJobIdAsync(long jobId, CancellationToken ct)
    {
        if (!await _db.Jobs.AnyAsync(j => j.Id == jobId, ct))
            return ApiResult<List<JobNoteDto>>.Fail("İş bulunamadı.", statusCode: 404);

        var items = await _db.JobNotes
            .AsNoTracking()
            .Include(n => n.CreatedByUser)
            .Where(n => n.JobId == jobId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => JobNoteDto.FromEntity(n))
            .ToListAsync(ct);

        return ApiResult<List<JobNoteDto>>.Ok(items, "Notlar listelendi.");
    }

    public async Task<ApiResult<JobNoteDto>> CreateAsync(long jobId, CreateJobNoteRequest request, Guid userId, CancellationToken ct)
    {
        if (!await _db.Jobs.AnyAsync(j => j.Id == jobId, ct))
            return ApiResult<JobNoteDto>.Fail("İş bulunamadı.", statusCode: 404);

        var entity = new Domain.Entities.JobNote
        {
            JobId = jobId,
            NoteText = request.NoteText.Trim(),
            CreatedByUserId = userId
        };

        _db.JobNotes.Add(entity);
        await _db.SaveChangesAsync(ct);

        var created = await _db.JobNotes
            .AsNoTracking()
            .Include(n => n.CreatedByUser)
            .FirstAsync(n => n.Id == entity.Id, ct);

        return ApiResult<JobNoteDto>.Ok(JobNoteDto.FromEntity(created), "Not eklendi.", 201);
    }

    public async Task<ApiResult<JobNoteDto>> UpdateAsync(long id, UpdateJobNoteRequest request, CancellationToken ct)
    {
        var entity = await _db.JobNotes.FirstOrDefaultAsync(n => n.Id == id, ct);
        if (entity is null)
            return ApiResult<JobNoteDto>.Fail("Not bulunamadı.", statusCode: 404);

        entity.NoteText = request.NoteText.Trim();
        await _db.SaveChangesAsync(ct);

        var updated = await _db.JobNotes
            .AsNoTracking()
            .Include(n => n.CreatedByUser)
            .FirstAsync(n => n.Id == entity.Id, ct);

        return ApiResult<JobNoteDto>.Ok(JobNoteDto.FromEntity(updated), "Not güncellendi.");
    }

    public async Task<ApiResult<bool>> DeleteAsync(long id, CancellationToken ct)
    {
        var entity = await _db.JobNotes.FirstOrDefaultAsync(n => n.Id == id, ct);
        if (entity is null)
            return ApiResult<bool>.Fail("Not bulunamadı.", statusCode: 404);

        entity.IsDeleted = true;
        await _db.SaveChangesAsync(ct);

        return ApiResult<bool>.Ok(true, "Not silindi.");
    }
}
