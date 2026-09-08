namespace PrivacyComply.Application.Abstractions.Identity;

public interface IAuthenticationSessionQueries
{
    Task<AuthenticationSessionRecord?> GetActiveByReferenceAsync(
        string sessionReference,
        CancellationToken cancellationToken = default);
}