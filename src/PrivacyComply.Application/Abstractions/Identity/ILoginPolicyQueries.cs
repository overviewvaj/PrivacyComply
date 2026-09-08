namespace PrivacyComply.Application.Abstractions.Identity;

public interface ILoginPolicyQueries
{
    Task<LoginPolicyRecord?> GetActiveAsync(
        CancellationToken cancellationToken = default);
}

public sealed record LoginPolicyRecord(
    Guid LoginPolicyId,
    string PolicyCode,
    string PolicyName,
    int MinimumPasswordLength,
    int MaximumFailedSignInAttempts,
    int LockoutDurationMinutes,
    int SessionLifetimeMinutes,
    int PasswordHistoryCount,
    int? PasswordMaximumAgeDays,
    bool RequireMfa,
    bool RequireMfaForPlatformUsers,
    bool RequireEmailConfirmation);