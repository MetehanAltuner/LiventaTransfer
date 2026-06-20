using System.IdentityModel.Tokens.Jwt;
using System.Text;
using FluentValidation;
using LiventaTransfer.API.Common;
using LiventaTransfer.API.Filters;
using LiventaTransfer.API.Hubs;
using LiventaTransfer.API.Middleware;
using LiventaTransfer.API.Realtime;
using LiventaTransfer.Application.Common;
using LiventaTransfer.Application.Interfaces;
using LiventaTransfer.Application.Interfaces.Services;
using LiventaTransfer.Application.Services;
using LiventaTransfer.Application.Validators;
using LiventaTransfer.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

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

        // Require authentication on every endpoint by default.
        // Opt out per-endpoint with [AllowAnonymous] (login, register, ws-test, health).
        var requireAuth = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
        options.Filters.Add(new AuthorizeFilter(requireAuth));
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = ModelBindingErrorFormatter.Format(context.ModelState);
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

// JWT Bearer authentication
var jwtKey = cfg["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
var jwtIssuer = cfg["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
var jwtAudience = cfg["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience is not configured.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = "role"
        };

        // SignalR sends the bearer token via ?access_token=... on the hub URL.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"];
                var path = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    ctx.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

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

// Realtime (SignalR)
builder.Services.AddSignalR();
builder.Services.AddSingleton<IJobBroadcaster, JobBroadcaster>();

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

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT bearer token. Format: Bearer {token}"
        };

        var bearerRef = new OpenApiSecuritySchemeReference("Bearer", document);
        document.Security =
        [
            new OpenApiSecurityRequirement { [bearerRef] = new List<string>() }
        ];

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

// Static files (wwwroot — serves /ws-test.html etc.)
app.UseStaticFiles();

// CORS
app.UseCors();

// AuthN/AuthZ
app.UseAuthentication();
app.UseAuthorization();

// OpenAPI + Scalar
app.MapOpenApi();
app.MapScalarApiReference();

app.MapControllers();

// Realtime hub — clients subscribe to /hubs/jobs and listen for "JobListEvent" messages.
// WebSocket endpoint is intentionally anonymous (per requirement).
app.MapHub<JobsHub>("/hubs/jobs").AllowAnonymous();

// Health check endpoint
app.MapGet("/", () => Results.Ok(new { Status = "Running", Service = "LiventaTransfer API" }))
    .AllowAnonymous();

// Seed database
await DataSeeder.SeedAsync(app.Services);

app.Run();
