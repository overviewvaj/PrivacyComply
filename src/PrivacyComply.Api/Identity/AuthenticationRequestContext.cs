using PrivacyComply.Application.Abstractions.Identity;

namespace PrivacyComply.Api.Identity;

public sealed class AuthenticationRequestContext
    : IAuthenticationRequestContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthenticationRequestContext(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private HttpContext? HttpContext =>
        _httpContextAccessor.HttpContext;

    public Guid? CorrelationId
    {
        get
        {
            var httpContext = HttpContext;

            if (httpContext is null)
            {
                return null;
            }

            if (httpContext.Items.TryGetValue(
                    "CorrelationId",
                    out var correlationIdValue))
            {
                if (correlationIdValue is Guid correlationId)
                {
                    return correlationId;
                }

                if (correlationIdValue is string correlationIdString &&
                    Guid.TryParse(
                        correlationIdString,
                        out var parsedCorrelationId))
                {
                    return parsedCorrelationId;
                }
            }

            var headerValue =
                httpContext.Request.Headers[
                    "X-Correlation-Id"]
                    .FirstOrDefault();

            return Guid.TryParse(
                headerValue,
                out var headerCorrelationId)
                    ? headerCorrelationId
                    : null;
        }
    }

    public string? ClientIpAddress
    {
        get
        {
            var ipAddress =
                HttpContext?
                    .Connection
                    .RemoteIpAddress?
                    .ToString();

            return string.IsNullOrWhiteSpace(ipAddress)
                ? null
                : ipAddress;
        }
    }

    public string? ClientUserAgent
    {
        get
        {
            var userAgent =
                HttpContext?
                    .Request
                    .Headers
                    .UserAgent
                    .ToString();

            if (string.IsNullOrWhiteSpace(userAgent))
            {
                return null;
            }

            return userAgent.Length <= 1000
                ? userAgent
                : userAgent[..1000];
        }
    }
}