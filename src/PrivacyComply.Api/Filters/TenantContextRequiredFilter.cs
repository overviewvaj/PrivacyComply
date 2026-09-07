using PrivacyComply.Application.Abstractions.Tenancy;

namespace PrivacyComply.Api.Filters;

public sealed class TenantContextRequiredFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var tenantContext =
            context.HttpContext.RequestServices
                .GetRequiredService<ITenantContext>();

        if (!tenantContext.HasTenant)
        {
            return Results.BadRequest(new
            {
                error = "TENANT_CONTEXT_REQUIRED",
                message = "A valid organisation context is required."
            });
        }

        return await next(context);
    }
}