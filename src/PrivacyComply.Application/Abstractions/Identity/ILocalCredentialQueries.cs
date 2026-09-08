namespace PrivacyComply.Application.Abstractions.Identity;

public interface ILocalCredentialQueries
{
    Task<LocalCredentialRecord?> GetActiveAsync(
        Guid userAccountId,
        CancellationToken cancellationToken = default);
}

public sealed record LocalCredentialRecord(
    Guid LocalCredentialId,
    Guid UserAccountId,
    string PasswordHash,
    string PasswordAlgorithmCode,
    int CredentialVersion,
    bool MustChangePassword,
    bool IsActive);