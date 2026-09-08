using System.Security.Claims;

using PrivacyComply.Application.Features.Identity.Services;

namespace PrivacyComply.Api.Identity;

public static class AuthenticatedPrincipalReader
{
    private const string AuthenticationSessionIdClaim =
        "authentication_session_id";

    private const string SessionReferenceClaim =
        "session_reference";

    private const string IsPlatformUserClaim =
        "is_platform_user";

    public static AuthenticationSessionRequest
        CreateSessionRequest(
            ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        if (
            principal.Identity?.IsAuthenticated
            != true)
        {
            throw new InvalidOperationException(
                "The request principal is not authenticated.");
        }

        var userAccountId =
            GetRequiredGuidClaim(
                principal,
                ClaimTypes.NameIdentifier);

        var authenticationSessionId =
            GetRequiredGuidClaim(
                principal,
                AuthenticationSessionIdClaim);

        var sessionReference =
            GetRequiredClaim(
                principal,
                SessionReferenceClaim);

        var emailAddress =
            GetRequiredClaim(
                principal,
                ClaimTypes.Email);

        var displayName =
            GetRequiredClaim(
                principal,
                ClaimTypes.Name);

        var isPlatformUserValue =
            GetRequiredClaim(
                principal,
                IsPlatformUserClaim);

        if (
            !bool.TryParse(
                isPlatformUserValue,
                out var isPlatformUser))
        {
            throw new InvalidOperationException(
                "The authenticated principal contains an " +
                "invalid platform-user claim.");
        }

        return new AuthenticationSessionRequest(
            userAccountId,
            authenticationSessionId,
            sessionReference,
            emailAddress,
            displayName,
            isPlatformUser);
    }

    private static string GetRequiredClaim(
        ClaimsPrincipal principal,
        string claimType)
    {
        var value =
            principal.FindFirstValue(
                claimType);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"The authenticated principal does not contain " +
                $"the required '{claimType}' claim.");
        }

        return value;
    }

    private static Guid GetRequiredGuidClaim(
        ClaimsPrincipal principal,
        string claimType)
    {
        var value =
            GetRequiredClaim(
                principal,
                claimType);

        if (
            !Guid.TryParse(
                value,
                out var parsedValue)
            ||
            parsedValue == Guid.Empty)
        {
            throw new InvalidOperationException(
                $"The authenticated principal contains an " +
                $"invalid '{claimType}' claim.");
        }

        return parsedValue;
    }
}