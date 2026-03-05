using System.Text;
using FluentValidation;
using LiventaTransfer.API.Filters;
using LiventaTransfer.API.Middleware;
using LiventaTransfer.Application.Common;
using LiventaTransfer.Application.Interfaces;
using LiventaTransfer.Application.Interfaces.Services;
using LiventaTransfer.Application.Services;
using LiventaTransfer.Application.Validators;
using LiventaTransfer.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var cfg = builder.Configuration;

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
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(cfg.GetConnectionString("DefaultConnection")));

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

// JWT Authentication
var jwtKey = cfg["Jwt:Key"]!;
var jwtIssuer = cfg["Jwt:Issuer"]!;
var jwtAudience = cfg["Jwt:Audience"]!;

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorization();

// OpenAPI
builder.Services.AddOpenApi();

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

// Global exception handler
app.UseMiddleware<GlobalExceptionHandler>();

// Serilog request logging
app.UseSerilogRequestLogging();

// CORS
app.UseCors();

// Auth
app.UseAuthentication();
app.UseAuthorization();

// OpenAPI + Scalar
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapControllers();

// Health check endpoint
app.MapGet("/", () => Results.Ok(new { Status = "Running", Service = "LiventaTransfer API" }));

// Seed database
await DataSeeder.SeedAsync(app.Services);

app.Run();
