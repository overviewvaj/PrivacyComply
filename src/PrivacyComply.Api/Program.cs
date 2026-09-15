using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PrivacyComply.Api.Authentication;
using PrivacyComply.Api.Endpoints.Development;
using PrivacyComply.Api.Endpoints.Identity;
using PrivacyComply.Api.Endpoints.Retention;
using PrivacyComply.Api.ExceptionHandling;
using PrivacyComply.Api.Identity;
using PrivacyComply.Api.Middleware;
using PrivacyComply.Api.Observability;
using PrivacyComply.Api.Security;
using PrivacyComply.Api.Tenancy;
using PrivacyComply.Application.Abstractions.Audit;
using PrivacyComply.Application.Abstractions.Identity;
using PrivacyComply.Application.Abstractions.Observability;
using PrivacyComply.Application.Abstractions.Security;
using PrivacyComply.Application.Abstractions.Tenancy;
using PrivacyComply.Application.Features.Identity.Services;
using PrivacyComply.Application.Features.Retention.Queries;
using PrivacyComply.Application.Features.Retention.Services;
using PrivacyComply.Infrastructure.Audit;
using PrivacyComply.Infrastructure.Database;
using PrivacyComply.Infrastructure.Identity;
using PrivacyComply.Infrastructure.Retention.Queries;
using PrivacyComply.Infrastructure.Retention.Services;
using Serilog;
using System.Text;
using PrivacyComply.Application.Features.Tenancy.Services;
using PrivacyComply.Application.Features.Runs.Queries;
using PrivacyComply.Infrastructure.Runs.Queries;
using PrivacyComply.Api.Endpoints.Runs;
using PrivacyComply.Application.Features.Runs.Commands;
using PrivacyComply.Infrastructure.Runs.Commands;
using PrivacyComply.Api.Endpoints.Security;


var builder = WebApplication.CreateBuilder(args);

// ------------------------------------------------------------
// Structured Logging
// ------------------------------------------------------------

// Each application startup creates a uniquely timestamped log file.
//
// Example:
// privacycomply-20260908-164500.log
// privacycomply-error-20260908-164500.log
var logFileTimestamp =
    DateTime.Now.ToString("yyyyMMdd-HHmmss");

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
        path:
            $"logs/errors/privacycomply-error-{logFileTimestamp}.log",
        restrictedToMinimumLevel:
            Serilog.Events.LogEventLevel.Error,
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
                &&
                securityEventValue
                    is Serilog.Events.ScalarValue scalarValue
                &&
                scalarValue.Value is bool isSecurityEvent
                &&
                isSecurityEvent)
            .WriteTo.File(
                path:
                    $"logs/security/privacycomply-security-{logFileTimestamp}.log",
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

// ------------------------------------------------------------
// Tenant Context
// ------------------------------------------------------------

// Tenant context is scoped to a single HTTP request.
//
// During the current development phase the tenant middleware
// supplies the OrganisationId.
//
// Once authentication is fully implemented, tenant identity
// will be resolved from trusted authenticated membership
// information rather than a client-supplied organisation ID.
builder.Services.AddScoped<TenantContext>();

builder.Services.AddScoped<ITenantContext>(serviceProvider =>
    serviceProvider.GetRequiredService<TenantContext>());

builder.Services.AddScoped<ITenantContextSetter>(serviceProvider =>
    serviceProvider.GetRequiredService<TenantContext>());

// ------------------------------------------------------------
// HTTP Context / Authenticated User Context
// ------------------------------------------------------------

// Provides access to the current ASP.NET Core HttpContext.
//
// Application services consume authenticated identity information
// through abstractions rather than depending directly on ASP.NET.
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<
    IAuthenticatedUserContext,
    AuthenticatedUserContext>();

builder.Services.AddScoped<
    IAuthenticationRequestContext,
    AuthenticationRequestContext>();

// ------------------------------------------------------------
// Identity / Authentication
// ------------------------------------------------------------

// Password hashing and verification.
//
// Plaintext passwords must never be persisted, logged,
// written to audit records, or returned by the API.
builder.Services.AddScoped<
    IPasswordHashService,
    PasswordHashService>();

// User account lookup.
builder.Services.AddScoped<
    IUserAccountQueries,
    UserAccountQueries>();

// Local credential persistence and lookup.
builder.Services.AddScoped<
    ILocalCredentialQueries,
    LocalCredentialQueries>();

builder.Services.AddScoped<
    ILocalCredentialCommands,
    LocalCredentialCommands>();

// Organisation membership bootstrap.
builder.Services.AddScoped<
    IOrganisationMembershipQueries,
    OrganisationMembershipQueries>();

// Organisation-scoped effective role/permission lookup.
builder.Services.AddScoped<
    IUserPermissionQueries,
    UserPermissionQueries>();

// Enterprise authentication policy.
builder.Services.AddScoped<
    ILoginPolicyQueries,
    LoginPolicyQueries>();

// User authentication state updates:
// successful login, failed login and lockout handling.
builder.Services.AddScoped<
    IUserAccountAuthenticationCommands,
    UserAccountAuthenticationCommands>();

// Authentication/security telemetry.
builder.Services.AddScoped<
    IAuthenticationEventCommands,
    AuthenticationEventCommands>();

// Authentication sessions.
builder.Services.AddScoped<
    IAuthenticationSessionCommands,
    AuthenticationSessionCommands>();

builder.Services.AddScoped<
    IAuthenticationSessionQueries,
    AuthenticationSessionQueries>();

// JWT token creation.
builder.Services.AddScoped<
    IAuthenticationTokenService,
    AuthenticationTokenService>();

// Main sign-in orchestration service.
builder.Services.AddScoped<
    ISignInService,
    SignInService>();

// Restores and validates an authenticated server-side session.
builder.Services.AddScoped<
    IAuthenticationSessionService,
    AuthenticationSessionService>();

builder.Services.AddScoped<
    ISignOutService,
    SignOutService>();

builder.Services.AddScoped<
    IAuthenticatedTenantResolver,
    AuthenticatedTenantResolver>();

// ------------------------------------------------------------
// JWT Configuration
// ------------------------------------------------------------

// Bind and validate JWT configuration.
//
// The development signing key currently comes from
// appsettings.Development.json.
//
// Production must use a secure secret provider/environment
// configuration rather than storing a signing key in source.
builder.Services
    .AddOptions<JwtAuthenticationOptions>()
    .Bind(
        builder.Configuration.GetSection(
            JwtAuthenticationOptions.SectionName))
    .Validate(
        options =>
            !string.IsNullOrWhiteSpace(options.Issuer),
        "JWT issuer is required.")
    .Validate(
        options =>
            !string.IsNullOrWhiteSpace(options.Audience),
        "JWT audience is required.")
    .Validate(
        options =>
            !string.IsNullOrWhiteSpace(options.SigningKey),
        "JWT signing key is required.")
    .Validate(
        options =>
            options.SigningKey.Length >= 32,
        "JWT signing key must be at least 32 characters.")
    .ValidateOnStart();

// ------------------------------------------------------------
// Authentication Cookie Configuration
// ------------------------------------------------------------

// The authentication cookie contains the short-lived
// PrivacyComply access token.
//
// The browser cannot read the cookie because it is HttpOnly.
// ASP.NET Core reads the token from the cookie and performs
// normal JWT validation.
//
// Production must continue to use Secure and HttpOnly cookies.
// SameSite configuration depends on the production deployment
// topology.
builder.Services
    .AddOptions<AuthenticationCookieOptions>()
    .Bind(
        builder.Configuration.GetSection(
            AuthenticationCookieOptions.SectionName))
    .Validate(
        options =>
            !string.IsNullOrWhiteSpace(options.CookieName),
        "Authentication cookie name is required.")
    .Validate(
        options =>
            options.CookieName.StartsWith(
                "__Host-",
                StringComparison.Ordinal),
        "Authentication cookie name must use the __Host- prefix.")
    .Validate(
        options =>
            options.Secure,
        "Authentication cookie must be Secure.")
    .Validate(
        options =>
            options.HttpOnly,
        "Authentication cookie must be HttpOnly.")
    .Validate(
        options =>
            !string.IsNullOrWhiteSpace(options.SameSite),
        "Authentication cookie SameSite setting is required.")
    .ValidateOnStart();

// ------------------------------------------------------------
// JWT Bearer Authentication
// ------------------------------------------------------------

// Configure ASP.NET Core to authenticate requests using
// PrivacyComply-issued JWT access tokens.
//
// For browser requests, the JWT is retrieved from the secure,
// HttpOnly authentication cookie.
//
// The normal Authorization: Bearer mechanism remains available
// when no PrivacyComply authentication cookie is present.
//
// Organisation/tenant identity is deliberately NOT derived from
// an untrusted request header here. Tenant resolution will be
// connected to authenticated membership separately.
var jwtAuthenticationOptions =
    builder.Configuration
        .GetSection(JwtAuthenticationOptions.SectionName)
        .Get<JwtAuthenticationOptions>()
    ?? throw new InvalidOperationException(
        "JWT authentication configuration is missing.");

var authenticationCookieOptions =
    builder.Configuration
        .GetSection(AuthenticationCookieOptions.SectionName)
        .Get<AuthenticationCookieOptions>()
    ?? new AuthenticationCookieOptions();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = true;

        options.Events =
            new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    if (
                        context.Request.Cookies.TryGetValue(
                            authenticationCookieOptions.CookieName,
                            out var accessToken)
                        &&
                        !string.IsNullOrWhiteSpace(accessToken))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                }
            };

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer =
                    jwtAuthenticationOptions.Issuer,

                ValidateAudience = true,
                ValidAudience =
                    jwtAuthenticationOptions.Audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            jwtAuthenticationOptions.SigningKey)),

                ValidateLifetime = true,

                ClockSkew = TimeSpan.FromMinutes(1)
            };
    });

builder.Services.AddAuthorization();

// ------------------------------------------------------------
// Cross-Site Request Forgery Protection
// ------------------------------------------------------------

// PrivacyComply authenticates browser requests using a secure,
// HttpOnly cookie. State-changing browser requests therefore
// require an independent antiforgery token.
//
// The authentication cookie proves who the user is.
// The antiforgery token proves that the request originated
// from the PrivacyComply frontend rather than from an
// unrelated malicious website.
//
// The antiforgery cookie contains no authentication token,
// customer data, or application data.
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";

    options.Cookie.Name =
        "__Host-PrivacyComply-Antiforgery";

    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy =
        CookieSecurePolicy.Always;

    options.Cookie.SameSite =
        SameSiteMode.None;

    options.Cookie.Path = "/";
});


// ------------------------------------------------------------
// Database
// ------------------------------------------------------------

// SQL Server connection factory.
//
// The factory reads the PrivacyComplyDatabase connection string
// from configuration.
//
// It also establishes or clears SQL Server SESSION_CONTEXT
// for OrganisationId on every opened pooled connection.
builder.Services.AddScoped<SqlConnectionFactory>();

// ------------------------------------------------------------
// Retention
// ------------------------------------------------------------

builder.Services.AddScoped<
    IRetentionPolicyQueries,
    RetentionPolicyQueries>();

builder.Services.AddScoped<
    IRetentionCoverageQueries,
    RetentionCoverageQueries>();

builder.Services.AddScoped<
    IRetentionService,
    RetentionService>();

// ------------------------------------------------------------
// Audit / Security / Observability
// ------------------------------------------------------------

builder.Services.AddScoped<
    IAuditEventWriter,
    SqlAuditEventWriter>();

builder.Services.AddScoped<
    ICorrelationContext,
    CorrelationContext>();

builder.Services.AddScoped<
    ISecurityAuditService,
    SecurityAuditService>();

builder.Services.AddScoped<
    IActorContext,
    ActorContext>();

// ------------------------------------------------------------
// Run Queries
// ------------------------------------------------------------

builder.Services.AddScoped<
    IAnalysisRunQueries,
    AnalysisRunQueries>();

builder.Services.AddScoped<
    PrivacyComply.Application.Features.Rules.IDpdpRulesEngine,
    PrivacyComply.Infrastructure.Rules.DpdpRulesEngine>();

builder.Services.AddScoped<
    IAnalysisRunCommands,
    AnalysisRunCommands>();

builder.Services.AddScoped<
    ICancelAnalysisRunCommand,
    CancelAnalysisRunCommand>();

// ------------------------------------------------------------
// Exception Handling
// ------------------------------------------------------------

builder.Services.AddExceptionHandler<
    GlobalExceptionHandler>();

builder.Services.AddProblemDetails();

// ------------------------------------------------------------
// CORS
// ------------------------------------------------------------

// Allows the local React/Vite development frontend to call
// the PrivacyComply API from its separate development origin.
//
// Credentials are required because browser authentication uses
// the secure HttpOnly cookie.
//
// This policy is applied only in Development below.
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "DevelopmentFrontend",
        policy =>
        {
            policy
                .WithOrigins(
                    "http://localhost:5173")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

// ------------------------------------------------------------
// OpenAPI
// ------------------------------------------------------------

builder.Services.AddOpenApi();

// ------------------------------------------------------------
// Build Application
// ------------------------------------------------------------

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

// Request logging wraps the exception handler.
// This ensures Serilog records the final HTTP status code
// after known exceptions have been translated.
app.UseSerilogRequestLogging();

// Central exception handling sits inside request logging.
// Known application exceptions are translated to appropriate
// HTTP responses before Serilog writes its completion event.
app.UseExceptionHandler();

// Authenticate the request before tenant resolution.
//
// JwtBearer authentication retrieves the access token from the
// PrivacyComply secure HttpOnly authentication cookie and builds
// the trusted authenticated principal used by tenant resolution.
app.UseAuthentication();

// Resolve the tenant only after authentication.
//
// The requested organisation slug is treated as untrusted input.
// TenantContextMiddleware verifies it against the authenticated
// user's active server-side organisation memberships before
// establishing the trusted OrganisationId used by SQL RLS.
app.UseMiddleware<TenantContextMiddleware>();

// Authorization executes after authentication and tenant
// resolution so organisation-scoped authorization can use
// the trusted tenant context.
app.UseAuthorization();

// ------------------------------------------------------------
// PrivacyComply API Endpoints
// ------------------------------------------------------------

// Root health/status endpoint.
app.MapGet(
    "/",
    () =>
    {
        return Results.Ok(
            new
            {
                application = "PrivacyComply.Api",
                status = "Running",
                environment =
                    app.Environment.EnvironmentName
            });
    });

app.MapRetentionEndpoints();

app.MapAuthenticationEndpoints();

app.MapDevelopmentIdentityEndpoints(
    app.Environment);

app.MapRunEndpoints();
app.MapAntiforgeryEndpoints();

app.Run();