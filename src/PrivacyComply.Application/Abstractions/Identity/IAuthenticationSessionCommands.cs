namespace PrivacyComply.Application.Abstractions.Identity;

public interface IAuthenticationSessionCommands
{
    Task<AuthenticationSessionRecord> CreateAsync(
        CreateAuthenticationSessionCommand command,
        CancellationToken cancellationToken = default);

    Task RevokeAsync(
        Guid authenticationSessionId,
        string revokedReasonCode,
        DateTime revokedDateTime,
        CancellationToken cancellationToken = default);
}

public sealed record CreateAuthenticationSessionCommand(
    Guid UserAccountId,
    string SessionReference,
    string AuthenticationMethodCode,
    DateTime IssuedDateTime,
    DateTime ExpiresDateTime,
    string? ClientIpAddress,
    string? ClientUserAgent);

public sealed record AuthenticationSessionRecord(
    Guid AuthenticationSessionId,
    Guid UserAccountId,
    string SessionReference,
    string SessionStatusCode,
    string AuthenticationMethodCode,
    DateTime IssuedDateTime,
    DateTime ExpiresDateTime,
    DateTime? LastActivityDateTime,
    DateTime? RevokedDateTime,
    string? RevokedReasonCode);