using PrivacyComply.Api.Endpoints.Retention;
using PrivacyComply.Api.ExceptionHandling;
using PrivacyComply.Api.Middleware;
using PrivacyComply.Api.Observability;
using PrivacyComply.Api.Security;
using PrivacyComply.Api.Tenancy;
using PrivacyComply.Application.Abstractions.Audit;
using PrivacyComply.Application.Abstractions.Observability;
using PrivacyComply.Application.Abstractions.Security;
using PrivacyComply.Application.Abstractions.Tenancy;
using PrivacyComply.Application.Features.Retention.Queries;
using PrivacyComply.Application.Features.Retention.Services;
using PrivacyComply.Infrastructure.Audit;
using PrivacyComply.Infrastructure.Database;
using PrivacyComply.Infrastructure.Retention.Queries;
using PrivacyComply.Infrastructure.Retention.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ------------------------------------------------------------
// Structured Logging
// ------------------------------------------------------------

// Each application startup creates a uniquely timestamped log file.
// Example:
// privacycomply-20260907-144500.log
// privacycomply-error-20260907-144500.log
var logFileTimestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override(
        "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware",
        Serilog.Events.LogEventLevel.Fatal)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console()
    .WriteTo.File(
        path: $"logs/privacycomply-{logFileTimestamp}.log",
        rollingInterval: RollingInterval.Infinite,
        retainedFileCountLimit: 30,
        shared: true,
        outputTemplate:
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} " +
            "[{Level:u3}] " +
            "[CorrelationId:{CorrelationId}] " +
            "[OrganisationId:{OrganisationId}] " +
            "{Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: $"logs/errors/privacycomply-error-{logFileTimestamp}.log",
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error,
        rollingInterval: RollingInterval.Infinite,
        retainedFileCountLimit: 90,
        shared: true,
        outputTemplate:
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} " +
            "[{Level:u3}] " +
            "[CorrelationId:{CorrelationId}] " +
            "[OrganisationId:{OrganisationId}] " +
            "{Message:lj}{NewLine}{Exception}")
    .WriteTo.Logger(loggerConfiguration =>
        loggerConfiguration
            .Filter.ByIncludingOnly(logEvent =>
                logEvent.Properties.TryGetValue(
                    "SecurityEvent",
                    out var securityEventValue)
                && securityEventValue is Serilog.Events.ScalarValue scalarValue
                && scalarValue.Value is bool isSecurityEvent
                && isSecurityEvent)
            .WriteTo.File(
                path: $"logs/security/privacycomply-security-{logFileTimestamp}.log",
                rollingInterval: RollingInterval.Infinite,
                retainedFileCountLimit: 180,
                shared: true,
                outputTemplate:
                    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} " +
                    "[{Level:u3}] " +
                    "[CorrelationId:{CorrelationId}] " +
                    "[OrganisationId:{OrganisationId}] " +
                    "{Message:lj}{NewLine}{Exception}"))
    .CreateLogger();

builder.Host.UseSerilog();

// ------------------------------------------------------------
// Dependency Injection
// ------------------------------------------------------------

// Tenant context is scoped to a single HTTP request.
// Later, the OrganisationId will be populated from the
// authenticated user's trusted tenant information.
builder.Services.AddScoped<TenantContext>();

builder.Services.AddScoped<ITenantContext>(serviceProvider =>
    serviceProvider.GetRequiredService<TenantContext>());

builder.Services.AddScoped<ITenantContextSetter>(serviceProvider =>
    serviceProvider.GetRequiredService<TenantContext>());

// SQL Server connection factory.
// This reads the PrivacyComplyDatabase connection string
// from application configuration.
builder.Services.AddScoped<SqlConnectionFactory>();

builder.Services.AddScoped<
    IRetentionPolicyQueries,
    RetentionPolicyQueries>();

builder.Services.AddScoped<
    IRetentionCoverageQueries,
    RetentionCoverageQueries>();

builder.Services.AddScoped<
    IRetentionService,
    RetentionService>();

builder.Services.AddScoped<
    IAuditEventWriter,
    SqlAuditEventWriter>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<
    ICorrelationContext,
    CorrelationContext>();

builder.Services.AddScoped<
    ISecurityAuditService,
    SecurityAuditService>();

builder.Services.AddScoped<
    IActorContext,
    ActorContext>();

// Central exception handling.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Provides the framework fallback response for any exception
// not explicitly handled by GlobalExceptionHandler.
builder.Services.AddProblemDetails();

// ------------------------------------------------------------
// CORS
// ------------------------------------------------------------

// Allows the local React/Vite development frontend to call
// the PrivacyComply API from its separate development origin.
//
// This policy is deliberately restricted to the known frontend
// origin rather than allowing arbitrary origins.
//
// The policy is being registered here only. It will be added to
// the HTTP request pipeline separately.
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ------------------------------------------------------------
// OpenAPI
// ------------------------------------------------------------

builder.Services.AddOpenApi();

var app = builder.Build();

// ------------------------------------------------------------
// HTTP Request Pipeline
// ------------------------------------------------------------

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseCors("DevelopmentFrontend");
}

// Establish correlation first so every downstream component,
// including exception handling and logging, can use it.
app.UseMiddleware<CorrelationIdMiddleware>();

// Resolve tenant context before logging and exception handling.
// This allows OrganisationId to be included in downstream logs.
app.UseMiddleware<TenantContextMiddleware>();

// Request logging wraps the exception handler.
// This ensures Serilog records the final HTTP status code
// after known exceptions have been translated.
app.UseSerilogRequestLogging();

// Central exception handling sits inside request logging.
// Known application exceptions are translated to appropriate
// HTTP responses before Serilog writes its completion event.
app.UseExceptionHandler();

// ------------------------------------------------------------
// PrivacyComply API Endpoints
// ------------------------------------------------------------

// Root health/status endpoint.
// This provides a simple confirmation that the API is running.
app.MapGet("/", () =>
{
    return Results.Ok(new
    {
        application = "PrivacyComply.Api",
        status = "Running",
        environment = app.Environment.EnvironmentName
    });
});

app.MapRetentionEndpoints();

app.Run();