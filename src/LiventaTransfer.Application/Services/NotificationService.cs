using LiventaTransfer.Application.Common;
using LiventaTransfer.Application.DTOs.Notification;
using LiventaTransfer.Application.Interfaces;
using LiventaTransfer.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LiventaTransfer.Application.Services;

public sealed class NotificationService
{
    private readonly IAppDbContext _db;
    public NotificationService(IAppDbContext db) => _db = db;

    public async Task<ApiResult<PagedResult<NotificationListDto>>> GetPagedAsync(
        PagedQuery query, NotificationChannel? channel, bool? isDelivered, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var q = _db.Notifications.AsNoTracking()
            .Include(n => n.Job)
            .Include(n => n.RecipientUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(n => n.Message.ToLower().Contains(query.Search.ToLower()));

        if (channel.HasValue)
            q = q.Where(n => n.Channel == channel.Value);

        if (isDelivered.HasValue)
            q = q.Where(n => n.IsDelivered == isDelivered.Value);

        var total = await q.LongCountAsync(ct);

        q = q.OrderByDescending(n => n.CreatedAt);

        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => NotificationListDto.FromEntity(n))
            .ToListAsync(ct);

        return ApiResult<PagedResult<NotificationListDto>>.Ok(new PagedResult<NotificationListDto>
        {
            Items = items, Page = page, PageSize = pageSize, TotalCount = total
        }, "Bildirimler listelendi.");
    }

    public async Task<ApiResult<NotificationListDto>> CreateAsync(CreateNotificationRequest request, CancellationToken ct)
    {
        var entity = new Domain.Entities.Notification
        {
            JobId = request.JobId,
            RecipientType = request.RecipientType,
            RecipientPhone = request.RecipientPhone?.Trim(),
            RecipientUserId = request.RecipientUserId,
            Channel = request.Channel,
            Message = request.Message.Trim(),
            SentAt = DateTime.UtcNow,
            IsDelivered = false
        };

        _db.Notifications.Add(entity);
        await _db.SaveChangesAsync(ct);

        var created = await _db.Notifications.AsNoTracking()
            .Include(n => n.Job)
            .Include(n => n.RecipientUser)
            .FirstAsync(n => n.Id == entity.Id, ct);

        return ApiResult<NotificationListDto>.Ok(NotificationListDto.FromEntity(created), "Bildirim oluşturuldu.", 201);
    }

    public async Task<ApiResult<bool>> MarkAsDeliveredAsync(long id, CancellationToken ct)
    {
        var entity = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id, ct);
        if (entity is null)
            return ApiResult<bool>.Fail("Bildirim bulunamadı.", statusCode: 404);

        entity.IsDelivered = true;
        entity.DeliveredAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return ApiResult<bool>.Ok(true, "Bildirim teslim edildi olarak işaretlendi.");
    }
}
