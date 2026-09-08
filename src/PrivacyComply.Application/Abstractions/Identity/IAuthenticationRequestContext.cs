namespace PrivacyComply.Application.Abstractions.Identity;

public interface IAuthenticationRequestContext
{
    Guid? CorrelationId { get; }

    string? ClientIpAddress { get; }

    string? ClientUserAgent { get; }
}