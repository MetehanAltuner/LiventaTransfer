using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using LiventaTransfer.Application.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LiventaTransfer.API.Middleware;

public class GlobalExceptionHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
            return;

        var (statusCode, message, errors) = MapException(exception);

        var result = ApiResult<object>.Fail(message, errors, statusCode);

        if (_env.IsDevelopment())
            result.Errors = (result.Errors ?? new List<string>())
                .Concat(new[] { exception.GetType().Name + ": " + exception.Message })
                .ToList();

        context.Response.Clear();
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(result, JsonOptions));
    }

    private static (int statusCode, string message, List<string>? errors) MapException(Exception ex)
    {
        switch (ex)
        {
            case DbUpdateException dbEx when dbEx.InnerException is PostgresException pg:
                return MapPostgres(pg);

            case DbUpdateConcurrencyException:
                return (409, "Kayıt başka bir işlem tarafından değiştirilmiş. Lütfen tekrar deneyin.", null);

            case DbUpdateException dbEx:
                return (400, "Veritabanı kaydı sırasında hata oluştu.", new List<string> { dbEx.InnerException?.Message ?? dbEx.Message });

            case FluentValidation.ValidationException vex:
                return (400, "Doğrulama hatası.", vex.Errors.Select(e => e.ErrorMessage).ToList());

            case ArgumentException aex:
                return (400, aex.Message, null);

            case UnauthorizedAccessException:
                return (401, "Yetkiniz yok.", null);

            case KeyNotFoundException knf:
                return (404, string.IsNullOrWhiteSpace(knf.Message) ? "Kayıt bulunamadı." : knf.Message, null);

            case OperationCanceledException:
                return (499, "İstek iptal edildi.", null);

            default:
                return (500, "Beklenmeyen bir hata oluştu.", new List<string> { ex.Message });
        }
    }

    private static (int statusCode, string message, List<string>? errors) MapPostgres(PostgresException pg)
    {
        // PostgreSQL SQLSTATE codes: https://www.postgresql.org/docs/current/errcodes-appendix.html
        switch (pg.SqlState)
        {
            case "23503": // foreign_key_violation
            {
                var entity = ExtractEntityFromConstraint(pg.ConstraintName);
                var msg = entity is not null
                    ? $"İlişkili kayıt bulunamadı: {entity}. Gönderilen ID veritabanında mevcut değil."
                    : "İlişkili bir kayıt bulunamadı. Gönderdiğiniz ID'lerden biri veritabanında mevcut değil.";
                return (400, msg, pg.ConstraintName is null ? null : new List<string> { pg.ConstraintName });
            }
            case "23505": // unique_violation
            {
                var entity = ExtractEntityFromUniqueConstraint(pg.ConstraintName, pg.TableName);
                var detail = ParseUniqueDetail(pg.Detail);

                string msg;
                if (detail is not null && entity is not null)
                    msg = $"{entity} kaydı zaten mevcut: {detail.Value.Field} = '{detail.Value.Value}'";
                else if (detail is not null)
                    msg = $"Bu değere sahip bir kayıt zaten mevcut: {detail.Value.Field} = '{detail.Value.Value}'";
                else if (entity is not null)
                    msg = $"{entity} kaydı zaten mevcut.";
                else
                    msg = "Bu kayıt zaten mevcut (benzersizlik kısıtı ihlali).";

                var errors = new List<string>();
                if (!string.IsNullOrWhiteSpace(pg.ConstraintName)) errors.Add(pg.ConstraintName);
                if (!string.IsNullOrWhiteSpace(pg.Detail)) errors.Add(pg.Detail);

                return (409, msg, errors.Count == 0 ? null : errors);
            }
            case "23502": // not_null_violation
                return (400, $"Zorunlu alan boş bırakılamaz: {pg.ColumnName ?? "(bilinmiyor)"}", null);
            case "23514": // check_violation
                return (400, "Geçersiz değer (kontrol kısıtı ihlali).",
                    pg.ConstraintName is null ? null : new List<string> { pg.ConstraintName });
            case "22001": // string_data_right_truncation
                return (400, "Bir alan veritabanında izin verilenden daha uzun.", null);
            default:
                return (400, "Veritabanı hatası: " + pg.MessageText, null);
        }
    }

    /// <summary>
    /// "FK_Jobs_Users_CreatedByUserId" gibi bir constraint adından insan-okur entity adını çıkarmaya çalışır.
    /// </summary>
    private static string? ExtractEntityFromConstraint(string? constraintName)
    {
        if (string.IsNullOrWhiteSpace(constraintName))
            return null;

        // Beklenen biçim: FK_<TabloA>_<TabloB>_<KolonAdı>
        var parts = constraintName.Split('_', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 4 && parts[0].Equals("FK", StringComparison.OrdinalIgnoreCase))
            return $"{parts[2]} ({parts[^1]})";

        return constraintName;
    }

    /// <summary>
    /// Unique constraint adı ya da tablo adından insan-okur tablo adını çıkarır.
    /// Örn: "IX_Customers_TaxNumber" → "Customers", tablo adı "Users" → "Users".
    /// </summary>
    private static string? ExtractEntityFromUniqueConstraint(string? constraintName, string? tableName)
    {
        if (!string.IsNullOrWhiteSpace(constraintName))
        {
            var parts = constraintName.Split('_', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 &&
                (parts[0].Equals("IX", StringComparison.OrdinalIgnoreCase) ||
                 parts[0].Equals("UX", StringComparison.OrdinalIgnoreCase) ||
                 parts[0].Equals("AK", StringComparison.OrdinalIgnoreCase)))
                return parts[1];
        }

        return string.IsNullOrWhiteSpace(tableName) ? null : tableName;
    }

    /// <summary>
    /// PostgreSQL "Key (column)=(value) already exists." formatındaki Detail string'inden
    /// kolon ve değer çıkarır.
    /// </summary>
    private static (string Field, string Value)? ParseUniqueDetail(string? detail)
    {
        if (string.IsNullOrWhiteSpace(detail))
            return null;

        var m = Regex.Match(detail, @"Key \(""?(?<col>[^""\)]+)""?\)=\((?<val>.*?)\) already exists",
            RegexOptions.IgnoreCase);
        if (!m.Success)
            return null;

        return (m.Groups["col"].Value, m.Groups["val"].Value);
    }
}
