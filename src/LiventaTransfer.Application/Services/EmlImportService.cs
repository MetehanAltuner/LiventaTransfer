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

            // Customer'ı bul veya oluştur
            var customerId = await FindOrCreateCustomerAsync(parsed.CustomerName, ct);

            // Passenger'ı bul veya oluştur
            var passengerId = await FindOrCreatePassengerAsync(
                parsed.Passenger.FullName, parsed.Passenger.Phone, parsed.Passenger.Email,
                customerId, ct);

            var result = parsed with
            {
                CustomerId = customerId,
                Passenger = parsed.Passenger with { Id = passengerId }
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
            passengerId = await FindOrCreatePassengerAsync(
                request.PassengerName, request.PassengerPhone, request.PassengerEmail,
                request.CustomerId, ct);
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
                        PassengerId = passengerId,
                        PassengerCount = 1,
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

    private async Task<long> FindOrCreateCustomerAsync(string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Müşteri adı boş olamaz.");

        var existing = await _db.Customers
            .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower().Trim(), ct);

        if (existing != null)
            return existing.Id;

        var customer = new Domain.Entities.Customer
        {
            Name = name.Trim(),
            CustomerType = CustomerType.Corporate,
            IsActive = true
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(ct);

        return customer.Id;
    }

    private async Task<long> FindOrCreatePassengerAsync(
        string fullName, string? phone, string? email, long customerId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new InvalidOperationException("Yolcu adı boş olamaz.");

        var existing = await _db.Passengers
            .FirstOrDefaultAsync(p => p.CustomerId == customerId &&
                                      p.FullName.ToLower() == fullName.ToLower().Trim(), ct);

        if (existing != null)
            return existing.Id;

        var passenger = new Domain.Entities.Passenger
        {
            FullName = fullName.Trim(),
            Phone = phone?.Trim(),
            Email = email?.Trim(),
            CustomerId = customerId,
            IsActive = true
        };

        _db.Passengers.Add(passenger);
        await _db.SaveChangesAsync(ct);

        return passenger.Id;
    }
}
