using Microsoft.AspNetCore.Antiforgery;

namespace PrivacyComply.Api.Endpoints.Security;

public static class AntiforgeryEndpoints
{
    public static IEndpointRouteBuilder MapAntiforgeryEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet(
                "/api/security/antiforgery-token",
                (
                    HttpContext httpContext,
                    IAntiforgery antiforgery) =>
                {
                    var tokens =
                        antiforgery.GetAndStoreTokens(
                            httpContext);

                    return Results.Ok(
                        new
                        {
                            requestToken =
                                tokens.RequestToken,

                            headerName =
                                tokens.HeaderName
                        });
                })
            .AllowAnonymous();

        return endpoints;
    }
}