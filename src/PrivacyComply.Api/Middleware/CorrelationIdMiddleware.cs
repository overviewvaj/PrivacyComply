using Serilog.Context;

namespace PrivacyComply.Api.Middleware;

public sealed class CorrelationIdMiddleware
{
    public const string CorrelationIdHeaderName = "X-Correlation-Id";
    public const string CorrelationIdItemKey = "PrivacyComply.CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        Guid correlationId;

        if (httpContext.Request.Headers.TryGetValue(
                CorrelationIdHeaderName,
                out var correlationIdHeader)
            && Guid.TryParse(
                correlationIdHeader.FirstOrDefault(),
                out var incomingCorrelationId)
            && incomingCorrelationId != Guid.Empty)
        {
            correlationId = incomingCorrelationId;
        }
        else
        {
            correlationId = Guid.NewGuid();
        }

        var correlationIdText =
            correlationId.ToString();

        // Store the strongly typed value for downstream application services.
        httpContext.Items[CorrelationIdItemKey] =
            correlationId;

        // Align ASP.NET request tracing with PrivacyComply's correlation ID.
        httpContext.TraceIdentifier =
            correlationIdText;

        // Return the correlation ID to the caller for diagnostics/support.
        httpContext.Response.Headers[
            CorrelationIdHeaderName] = correlationIdText;

        // Enrich every Serilog event created during this request.
        using (LogContext.PushProperty(
                   "CorrelationId",
                   correlationIdText))
        {
            await _next(httpContext);
        }
    }
}