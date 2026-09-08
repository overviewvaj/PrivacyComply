namespace PrivacyComply.Application.Abstractions.Identity;

public interface IUserAccountQueries
{
    Task<UserAccountRecord?> GetByNormalizedEmailAsync(
        string normalizedEmailAddress,
        CancellationToken cancellationToken = default);
}

public sealed record UserAccountRecord(
    Guid UserAccountId,
    string UserAccountCode,
    string EmailAddress,
    string NormalizedEmailAddress,
    string DisplayName,
    string AccountStatusCode,
    bool EmailConfirmed,
    bool IsPlatformUser,
    int FailedSignInCount,
    DateTime? LockoutEndDateTime,
    DateTime? LastSuccessfulSignInDateTime,
    DateTime? LastFailedSignInDateTime,
    DateTime? PasswordChangedDateTime,
    Guid SecurityStamp,
    bool IsDeleted);