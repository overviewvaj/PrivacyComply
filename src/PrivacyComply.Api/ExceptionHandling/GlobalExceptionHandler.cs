using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Diagnostics;
using PrivacyComply.Application.Features.Retention.Exceptions;

namespace PrivacyComply.Api.ExceptionHandling;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is RetentionPolicyNotFoundException)
        {
            httpContext.Response.StatusCode =
                StatusCodes.Status404NotFound;

            await httpContext.Response.WriteAsJsonAsync(
                new
                {
                    error = "RETENTION_POLICY_NOT_FOUND",
                    message =
                        "The retention policy was not found for the current organisation."
                },
                cancellationToken);

            return true;
        }

        if (exception is AntiforgeryValidationException)
        {
            httpContext.Response.StatusCode =
                StatusCodes.Status400BadRequest;

            await httpContext.Response.WriteAsJsonAsync(
                new
                {
                    error = "ANTIFORGERY_VALIDATION_FAILED",
                    message =
                        "The request could not be validated. Refresh the page and try again."
                },
                cancellationToken);

            return true;
        }

        _logger.LogError(
            exception,
            "Unhandled exception occurred while processing HTTP {Method} {Path}.",
            httpContext.Request.Method,
            httpContext.Request.Path);

        return false;
    }
}