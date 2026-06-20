using LiventaTransfer.Application.Common;
using LiventaTransfer.Application.DTOs.EmlImport;
using LiventaTransfer.Application.DTOs.Job;
using LiventaTransfer.Application.Interfaces;
using LiventaTransfer.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LiventaTransfer.Application.Services;

public sealed class EmlImportService
{
    private readonly IAppDbContext _db;
    private readonly TatilsepetiEmlParserService _parser;
    private readonly JobService _jobService;

    public EmlImportService(IAppDbContext db, TatilsepetiEmlParserService parser, JobService jobService)
    {
        _db = db;
        _parser = parser;
        _jobService = jobService;
    }

    public async Task<ApiResult<EmlParseResultDto>> ParseAsync(Stream emlStream, CancellationToken ct)
    {
        try
        {
            var parsed = await _parser.ParseAsync(emlStream, ct);

            if (parsed.Transfers.Count == 0)
                return ApiResult<EmlParseResultDto>.Fail(
                    "Mail içeriğinden geçerli transfer bilgisi bulunamadı. Lokasyonları dolu olan en az bir transfer gereklidir.");

            // Müşteri: varsa mevcudu kullan, yoksa kayıt aç. İsim boşsa atla.
            var (customerId, customerIsExisting, customerName) =
                await FindOrCreateCustomerAsync(parsed.CustomerName, ct);

            // Yolcu: varsa mevcudu kullan, yoksa kayıt aç. Ad boşsa atla.
            var passenger = await FindOrCreatePassengerAsync(
                parsed.Passenger.FullName, parsed.Passenger.Phone, parsed.Passenger.Email, ct);

            var result = parsed with
            {
                CustomerId = customerId,
                CustomerName = customerName,
                IsExistingCustomer = customerIsExisting,
                Passenger = passenger
            };

            return ApiResult<EmlParseResultDto>.Ok(result, $"{result.Transfers.Count} adet transfer bilgisi bulundu.");
        }
        catch (Exception ex)
        {
            return ApiResult<EmlParseResultDto>.Fail($"EML dosyası parse edilemedi: {ex.Message}");
        }
    }

    public async Task<ApiResult<EmlImportResultDto>> ConfirmAndCreateJobsAsync(
        ConfirmEmlImportRequest request, CancellationToken ct)
    {
        if (!await _db.Customers.AnyAsync(c => c.Id == request.CustomerId, ct))
            return ApiResult<EmlImportResultDto>.Fail("Müşteri bulunamadı.", statusCode: 400);

        if (request.Transfers.Count == 0)
            return ApiResult<EmlImportResultDto>.Fail("En az bir transfer gereklidir.", statusCode: 400);

        // PassengerId verilmişse direkt kullan, yoksa isimle bul/oluştur
        var passengerId = request.PassengerId;
        if (!passengerId.HasValue && !string.IsNullOrWhiteSpace(request.PassengerName))
        {
            var p = await FindOrCreatePassengerAsync(
                request.PassengerName, request.PassengerPhone, request.PassengerEmail, ct);
            passengerId = p.Id == 0 ? null : p.Id;
        }

        var createdJobs = new List<JobDetailDto>();

        foreach (var transfer in request.Transfers)
        {
            var createRequest = new CreateJobRequest
            {
                JobDate = transfer.JobDate,
                JobTime = transfer.JobTime,
                JobType = JobType.Transfer,
                SourceEmail = request.SourceEmail,
                Stops =
                [
                    new JobStopRequest
                    {
                        CustomerId = request.CustomerId,
                        PassengerIds = passengerId.HasValue ? [passengerId.Value] : [],
                        PickupAddress = transfer.PickupAddress,
                        DropoffAddress = transfer.DropoffAddress,
                        FlightCode = transfer.FlightCode,
                        Notes = transfer.Notes
                    }
                ]
            };

            var result = await _jobService.CreateAsync(createRequest, request.UserId, ct);
            if (result.Success && result.Data != null)
                createdJobs.Add(result.Data);
        }

        if (createdJobs.Count == 0)
            return ApiResult<EmlImportResultDto>.Fail("Hiçbir iş oluşturulamadı.");

        return ApiResult<EmlImportResultDto>.Ok(
            new EmlImportResultDto { CreatedJobs = createdJobs },
            $"{createdJobs.Count} adet iş başarıyla oluşturuldu.");
    }

    /// <summary>
    /// Müşteriyi adına göre arar; varsa mevcudu, yoksa yeni kaydı döner.
    /// İsim boşsa hiç dokunmaz (Id = 0).
    /// </summary>
    private async Task<(long Id, bool IsExisting, string Name)> FindOrCreateCustomerAsync(string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
            return (0, false, string.Empty);

        var trimmed = name.Trim();
        var existing = await _db.Customers
            .FirstOrDefaultAsync(c => c.Name.ToLower() == trimmed.ToLower(), ct);

        if (existing != null)
            return (existing.Id, true, existing.Name);

        var customer = new Domain.Entities.Customer
        {
            Name = trimmed,
            CustomerType = CustomerType.Corporate,
            IsActive = true
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(ct);

        return (customer.Id, false, customer.Name);
    }

    /// <summary>
    /// Yolcuyu adına göre arar; varsa mevcudu (gerçek alanlarıyla), yoksa yeni kaydı döner.
    /// Ad boşsa hiç dokunmaz (Id = 0).
    /// </summary>
    private async Task<EmlPassengerDto> FindOrCreatePassengerAsync(
        string fullName, string? phone, string? email, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return new EmlPassengerDto
            {
                Id = 0,
                FullName = string.Empty,
                Phone = phone?.Trim(),
                Email = email?.Trim(),
                IsExisting = false
            };
        }

        var trimmed = fullName.Trim();
        var existing = await _db.Passengers
            .FirstOrDefaultAsync(p => p.FullName.ToLower() == trimmed.ToLower(), ct);

        if (existing != null)
        {
            return new EmlPassengerDto
            {
                Id = existing.Id,
                FullName = existing.FullName,
                Phone = existing.Phone,
                Email = existing.Email,
                IsExisting = true
            };
        }

        var passenger = new Domain.Entities.Passenger
        {
            FullName = trimmed,
            Phone = phone?.Trim(),
            Email = email?.Trim(),
            IsActive = true
        };

        _db.Passengers.Add(passenger);
        await _db.SaveChangesAsync(ct);

        return new EmlPassengerDto
        {
            Id = passenger.Id,
            FullName = passenger.FullName,
            Phone = passenger.Phone,
            Email = passenger.Email,
            IsExisting = false
        };
    }
}
