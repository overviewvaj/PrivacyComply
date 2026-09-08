namespace PrivacyComply.Application.Abstractions.Identity;

public interface IAuthenticationEventCommands
{
    Task RecordAsync(
        AuthenticationEventRecord authenticationEvent,
        CancellationToken cancellationToken = default);
}

public sealed record AuthenticationEventRecord(
    Guid? UserAccountId,
    Guid? AuthenticationSessionId,
    string EventTypeCode,
    string EventStatusCode,
    string? AuthenticationMethodCode,
    string? EmailAddressReference,
    string? FailureReasonCode,
    Guid? CorrelationId,
    string? ClientIpAddress,
    string? ClientUserAgent,
    DateTime EventDateTime);