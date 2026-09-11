using System.Security.Claims;
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
        ITenantContextSetter tenantContextSetter,
        IAuthenticatedTenantResolver authenticatedTenantResolver)
    {
        Guid? organisationId = null;

        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            var userAccountIdValue =
                httpContext.User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            var organisationSlug =
                httpContext.Request.RouteValues[
                    "organisationSlug"]?.ToString();

            if (Guid.TryParse(
                    userAccountIdValue,
                    out var userAccountId)
                && userAccountId != Guid.Empty
                && !string.IsNullOrWhiteSpace(
                    organisationSlug))
            {
                var resolution =
                    await authenticatedTenantResolver
                        .ResolveAsync(
                            userAccountId,
                            organisationSlug,
                            httpContext.RequestAborted);

                if (!resolution.Succeeded
                    || !resolution.OrganisationId.HasValue)
                {
                    httpContext.Response.StatusCode =
                        StatusCodes.Status403Forbidden;

                    await httpContext.Response.WriteAsJsonAsync(
                        new
                        {
                            error = "organisation_access_denied"
                        },
                        httpContext.RequestAborted);

                    return;
                }

                organisationId =
                    resolution.OrganisationId.Value;

                tenantContextSetter.SetOrganisation(
                    organisationId.Value);
            }
        }

        using (LogContext.PushProperty(
                   "OrganisationId",
                   organisationId?.ToString()
                   ?? string.Empty))
        {
            await _next(httpContext);
        }
    }
}