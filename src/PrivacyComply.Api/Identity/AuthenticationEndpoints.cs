using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using PrivacyComply.Api.Authentication;
using PrivacyComply.Api.Identity;
using PrivacyComply.Application.Features.Identity.Services;

namespace PrivacyComply.Api.Endpoints.Identity;

public static class AuthenticationEndpoints
{
    public static IEndpointRouteBuilder MapAuthenticationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group =
            endpoints.MapGroup("/api/auth");

        // --------------------------------------------------------
        // Sign In
        // --------------------------------------------------------

        group.MapPost(
            "/sign-in",
            async (
                [FromBody] SignInRequest request,
                [FromServices] ISignInService signInService,
                [FromServices] IOptions<AuthenticationCookieOptions>
                    authenticationCookieOptions,
                HttpResponse httpResponse,
                CancellationToken cancellationToken) =>
            {
                var result =
                    await signInService.SignInAsync(
                        request,
                        cancellationToken);

                // MFA is a valid intermediate authentication state,
                // but the user is not fully authenticated yet.
                //
                // Therefore no authenticated session cookie must
                // be issued at this point.
                if (!result.Succeeded)
                {
                    if (string.Equals(
                            result.ResultCode,
                            "MFA_REQUIRED",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return Results.Ok(result);
                    }

                    return Results.Json(
                        result,
                        statusCode:
                            StatusCodes.Status401Unauthorized);
                }

                // A successful authentication must contain the
                // access token and expiry required to establish
                // the browser authentication cookie.
                if (
                    string.IsNullOrWhiteSpace(result.AccessToken)
                    ||
                    result.ExpiresDateTime is null)
                {
                    throw new InvalidOperationException(
                        "Successful authentication did not return " +
                        "the required access token or expiry.");
                }

                var cookieOptions =
                    authenticationCookieOptions.Value;

                var sameSiteMode =
                    ParseSameSiteMode(
                        cookieOptions.SameSite);

                httpResponse.Cookies.Append(
                    cookieOptions.CookieName,
                    result.AccessToken,
                    new CookieOptions
                    {
                        HttpOnly =
                            cookieOptions.HttpOnly,

                        Secure =
                            cookieOptions.Secure,

                        SameSite =
                            sameSiteMode,

                        Path = "/",

                        Expires =
                            new DateTimeOffset(
                                DateTime.SpecifyKind(
                                    result.ExpiresDateTime.Value,
                                    DateTimeKind.Utc)),

                        IsEssential = true
                    });

                return Results.Ok(result);
            })
            .AllowAnonymous();

        // --------------------------------------------------------
        // Current Authentication Session
        // --------------------------------------------------------

        group.MapGet(
            "/session",
            async (
                HttpContext httpContext,
                [FromServices]
                    IAuthenticationSessionService
                        authenticationSessionService,
                CancellationToken cancellationToken) =>
            {
                // Reaching this endpoint requires successful
                // ASP.NET Core authentication.
                //
                // JwtBearer has already validated the token
                // signature, issuer, audience and lifetime before
                // this handler executes.
                var sessionRequest =
                    AuthenticatedPrincipalReader
                        .CreateSessionRequest(
                            httpContext.User);

                // Validate the corresponding server-side
                // authentication session and rebuild current
                // organisation memberships, roles and permissions.
                var result =
                    await authenticationSessionService
                        .GetCurrentSessionAsync(
                            sessionRequest,
                            cancellationToken);

                if (!result.IsAuthenticated)
                {
                    return Results.Json(
                        result,
                        statusCode:
                            StatusCodes.Status401Unauthorized);
                }

                return Results.Ok(result);
            })
            .RequireAuthorization();

        // --------------------------------------------------------
        // Sign Out
        // --------------------------------------------------------

        group.MapPost(
            "/sign-out",
            async (
                HttpContext httpContext,
                [FromServices]
                    ISignOutService signOutService,
                [FromServices]
                    IOptions<AuthenticationCookieOptions>
                        authenticationCookieOptions,
                CancellationToken cancellationToken) =>
            {
                // JwtBearer has already validated the token before
                // this handler executes. Read only the trusted
                // authenticated claims from the ClaimsPrincipal.
                var authenticationRequest =
                    AuthenticatedPrincipalReader
                        .CreateSessionRequest(
                            httpContext.User);

                // Revoke the corresponding server-side session.
                // This ensures that possession of the old cookie
                // alone is no longer sufficient to restore the
                // application session.
                await signOutService.SignOutAsync(
                    new SignOutRequest(
                        authenticationRequest.UserAccountId,
                        authenticationRequest.AuthenticationSessionId),
                    cancellationToken);

                var cookieOptions =
                    authenticationCookieOptions.Value;

                var sameSiteMode =
                    ParseSameSiteMode(
                        cookieOptions.SameSite);

                // Remove the browser authentication cookie using
                // the same cookie properties used when it was
                // originally issued.
                httpContext.Response.Cookies.Delete(
                    cookieOptions.CookieName,
                    new CookieOptions
                    {
                        HttpOnly =
                            cookieOptions.HttpOnly,

                        Secure =
                            cookieOptions.Secure,

                        SameSite =
                            sameSiteMode,

                        Path = "/",

                        IsEssential = true
                    });

                return Results.NoContent();
            })
            .RequireAuthorization();

        return endpoints;
    }

    private static SameSiteMode ParseSameSiteMode(
        string configuredValue)
    {
        if (string.Equals(
                configuredValue,
                "Strict",
                StringComparison.OrdinalIgnoreCase))
        {
            return SameSiteMode.Strict;
        }

        if (string.Equals(
                configuredValue,
                "Lax",
                StringComparison.OrdinalIgnoreCase))
        {
            return SameSiteMode.Lax;
        }

        if (string.Equals(
                configuredValue,
                "None",
                StringComparison.OrdinalIgnoreCase))
        {
            return SameSiteMode.None;
        }

        throw new InvalidOperationException(
            $"Unsupported authentication cookie SameSite value " +
            $"'{configuredValue}'.");
    }
}