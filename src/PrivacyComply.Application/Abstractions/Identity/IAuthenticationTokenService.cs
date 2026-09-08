namespace PrivacyComply.Application.Abstractions.Identity;

public interface IAuthenticationTokenService
{
    AuthenticationTokenResult CreateToken(
        AuthenticationTokenRequest request);
}

public sealed record AuthenticationTokenRequest(
    Guid UserAccountId,
    Guid AuthenticationSessionId,
    string SessionReference,
    string EmailAddress,
    string DisplayName,
    bool IsPlatformUser,
    DateTime IssuedDateTime,
    DateTime ExpiresDateTime);

public sealed record AuthenticationTokenResult(
    string AccessToken,
    string TokenType,
    DateTime ExpiresDateTime);