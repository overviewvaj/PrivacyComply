using PrivacyComply.Api.Middleware;
using PrivacyComply.Application.Abstractions.Observability;

namespace PrivacyComply.Api.Observability;

public sealed class CorrelationContext : ICorrelationContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationContext(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid CorrelationId
    {
        get
        {
            var httpContext =
                _httpContextAccessor.HttpContext;

            if (httpContext is null)
            {
                return Guid.Empty;
            }

            if (httpContext.Items.TryGetValue(
                    CorrelationIdMiddleware.CorrelationIdItemKey,
                    out var correlationIdValue)
                && correlationIdValue is Guid correlationId)
            {
                return correlationId;
            }

            return Guid.Empty;
        }
    }

    public bool HasCorrelationId =>
        CorrelationId != Guid.Empty;
}