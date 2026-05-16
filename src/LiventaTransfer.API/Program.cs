using FluentValidation;
using LiventaTransfer.API.Filters;
using LiventaTransfer.API.Middleware;
using LiventaTransfer.Application.Common;
using LiventaTransfer.Application.Interfaces;
using LiventaTransfer.Application.Interfaces.Services;
using LiventaTransfer.Application.Services;
using LiventaTransfer.Application.Validators;
using LiventaTransfer.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var cfg = builder.Configuration;
var basePath = cfg["BasePath"];

// Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Controllers + FluentValidation filter + ModelState override
builder.Services.AddControllers(options =>
    {
        options.Filters.Add<FluentValidationFilter>();
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            var payload = ApiResult<object>.Fail("Doğrulama hatası.", errors, statusCode: 400);
            return new BadRequestObjectResult(payload);
        };
    });

builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

// DbContext
var connectionString = Environment.GetEnvironmentVariable("Transferapi.ConnectionString")
    ?? cfg.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Database connection string is not configured. Set the Transferapi.ConnectionString environment variable.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

// Auth services
builder.Services.AddScoped<IAuthService, AuthService>();

// CRUD services
builder.Services.AddScoped<BranchService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<PassengerService>();
builder.Services.AddScoped<VehicleOwnerService>();
builder.Services.AddScoped<VehicleService>();
builder.Services.AddScoped<DriverService>();
builder.Services.AddScoped<LocationService>();
builder.Services.AddScoped<JobService>();
builder.Services.AddScoped<JobNoteService>();
builder.Services.AddScoped<TripLogService>();
builder.Services.AddScoped<InvoiceService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<PermissionService>();

// EML Import services
builder.Services.AddScoped<TatilsepetiEmlParserService>();
builder.Services.AddScoped<EmlImportService>();
builder.Services.AddScoped<ConfirmationTableService>();

// OpenAPI
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        if (!string.IsNullOrWhiteSpace(basePath))
        {
            var normalizedBasePath = basePath.StartsWith('/') ? basePath : $"/{basePath}";
            document.Servers = new List<OpenApiServer>
            {
                new() { Url = normalizedBasePath }
            };
        }

        return Task.CompletedTask;
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

if (!string.IsNullOrWhiteSpace(basePath))
{
    var normalizedBasePath = basePath.StartsWith('/') ? basePath : $"/{basePath}";
    app.UsePathBase(normalizedBasePath);
}

// Global exception handler
app.UseMiddleware<GlobalExceptionHandler>();

// Serilog request logging
app.UseSerilogRequestLogging();

// CORS
app.UseCors();

// OpenAPI + Scalar
app.MapOpenApi();
app.MapScalarApiReference();

app.MapControllers();

// Health check endpoint
app.MapGet("/", () => Results.Ok(new { Status = "Running", Service = "LiventaTransfer API" }));

// Seed database
await DataSeeder.SeedAsync(app.Services);

app.Run();
