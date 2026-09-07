using PrivacyComply.Application.Abstractions.Tenancy;
using Serilog.Context;

namespace PrivacyComply.Api.Middleware;

public sealed class TenantContextMiddleware
{
    private readonly RequestDelegate _next;

    public TenantContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext httpContext,
        ITenantContextSetter tenantContextSetter)
    {
        // Temporary development-only tenant resolution.
        // Production tenant identity will later come from trusted
        // authenticated server-side context and must never rely on
        // a client-supplied organisation identifier.

        Guid? organisationId = null;

        if (httpContext.Request.Headers.TryGetValue(
                "X-Organisation-Id",
                out var organisationIdHeader)
            && Guid.TryParse(
                organisationIdHeader.FirstOrDefault(),
                out var parsedOrganisationId)
            && parsedOrganisationId != Guid.Empty)
        {
            tenantContextSetter.SetOrganisation(parsedOrganisationId);
            organisationId = parsedOrganisationId;
        }

        using (LogContext.PushProperty(
                   "OrganisationId",
                   organisationId?.ToString() ?? string.Empty))
        {
            await _next(httpContext);
        }
    }
}