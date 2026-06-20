using System.Text.Json;
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

            case DbUpdateException:
                return (400, "Kayıt veritabanına işlenirken bir hata oluştu. Lütfen verileri kontrol edip tekrar deneyin.", null);

            case FluentValidation.ValidationException vex:
                return (400, "Doğrulama hatası.", vex.Errors.Select(e => e.ErrorMessage).ToList());

            case ArgumentException aex:
                return (400, aex.Message, null);

            case UnauthorizedAccessException:
                return (401, "Bu işlem için yetkiniz yok.", null);

            case KeyNotFoundException knf:
                return (404, string.IsNullOrWhiteSpace(knf.Message) ? "Kayıt bulunamadı." : knf.Message, null);

            case OperationCanceledException:
                return (499, "İstek iptal edildi.", null);

            default:
                return (500, "Beklenmeyen bir hata oluştu. Lütfen daha sonra tekrar deneyin.", null);
        }
    }

    private static (int statusCode, string message, List<string>? errors) MapPostgres(PostgresException pg)
    {
        // PostgreSQL SQLSTATE codes: https://www.postgresql.org/docs/current/errcodes-appendix.html
        // Not: Ham constraint/detail bilgisi istemciye dönülmez (anlamsız ve hassas olabilir);
        // tüm exception zaten sunucu loglarına yazılıyor.
        switch (pg.SqlState)
        {
            case "23503": // foreign_key_violation
            {
                var noun = ReferencedTableNoun(pg.ConstraintName);
                var msg = noun is not null
                    ? $"Seçtiğiniz {noun} sistemde bulunamadı. Lütfen geçerli bir kayıt seçin."
                    : "Seçtiğiniz ilişkili kayıtlardan biri sistemde bulunamadı. Lütfen seçimlerinizi kontrol edin.";
                return (400, msg, null);
            }
            case "23505": // unique_violation
                return (409, FriendlyUniqueMessage(pg.ConstraintName, pg.TableName), null);
            case "23502": // not_null_violation
                return (400, "Zorunlu bir alan boş bırakılamaz. Lütfen tüm gerekli alanları doldurun.", null);
            case "23514": // check_violation
                return (400, "Girilen değerlerden biri geçerli değil. Lütfen kontrol edip tekrar deneyin.", null);
            case "22001": // string_data_right_truncation
                return (400, "Bir alan izin verilen uzunluğu aşıyor. Lütfen kısaltın.", null);
            default:
                return (400, "Veritabanı işlemi sırasında bir hata oluştu. Lütfen tekrar deneyin.", null);
        }
    }

    /// <summary>
    /// Bilinen unique constraint'ler için özel Türkçe mesaj; bilinmiyorsa tablo adından üretilen
    /// genel bir mesaj döner.
    /// </summary>
    private static string FriendlyUniqueMessage(string? constraintName, string? tableName)
    {
        var known = constraintName switch
        {
            "IX_Jobs_JobNumber" => "Aynı iş numarasına sahip bir kayıt zaten var. Lütfen tekrar deneyin.",
            "IX_Users_Username" => "Bu kullanıcı adı zaten kullanımda. Lütfen farklı bir ad seçin.",
            "IX_Customers_TaxNumber" => "Bu vergi numarası başka bir müşteride kayıtlı.",
            "IX_Customers_TcKimlikNo" => "Bu TC Kimlik No başka bir müşteride kayıtlı.",
            "IX_JobStopPassengers_JobStopId_PassengerId" => "Aynı yolcu bu durağa zaten eklenmiş.",
            "IX_PassengerLocations_PassengerId_LocationId" => "Bu lokasyon yolcuya zaten tanımlı.",
            _ => null
        };
        if (known is not null)
            return known;

        var noun = TableNoun(tableName);
        return noun is not null
            ? $"Bu {noun} kaydı zaten mevcut (benzersiz alan tekrar ediyor)."
            : "Bu kayıt zaten mevcut (benzersiz bir alan tekrar ediyor).";
    }

    /// <summary>FK constraint adından ("FK_Jobs_Drivers_DriverId") referans verilen tablonun Türkçe adını döner.</summary>
    private static string? ReferencedTableNoun(string? constraintName)
    {
        if (string.IsNullOrWhiteSpace(constraintName))
            return null;

        // Beklenen biçim: FK_<KaynakTablo>_<ReferansTablo>_<Kolon>
        var parts = constraintName.Split('_', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 4 && parts[0].Equals("FK", StringComparison.OrdinalIgnoreCase))
            return TableNoun(parts[2]);

        return null;
    }

    /// <summary>Tablo adını insan-okur Türkçe isme çevirir.</summary>
    private static string? TableNoun(string? tableName) => tableName switch
    {
        "Jobs" => "iş",
        "JobStops" => "durak",
        "JobStopPassengers" => "durak yolcusu",
        "Customers" => "müşteri",
        "Drivers" => "şoför",
        "Users" => "kullanıcı",
        "Vehicles" => "araç",
        "VehicleOwners" => "araç sahibi",
        "Passengers" => "yolcu",
        "Locations" => "lokasyon",
        "PassengerLocations" => "yolcu lokasyonu",
        "Branches" => "şube",
        "Invoices" => "fatura",
        "InvoiceItems" => "fatura kalemi",
        _ => null
    };
}
