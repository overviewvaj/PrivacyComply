namespace PrivacyComply.Application.Features.Retention.Queries;

public interface IRetentionPolicyQueries
{
    Task<IReadOnlyList<RetentionPolicySummary>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid retentionPolicyId,
        CancellationToken cancellationToken = default);
}

public sealed record RetentionPolicySummary(
    Guid RetentionPolicyId,
    string RetentionPolicyCode,
    string RetentionPolicyName,
    int RetentionPeriodValue,
    string RetentionPeriodUnitCode,
    string RetentionTriggerCode,
    string ExpiryActionCode,
    bool ReviewRequired,
    string StatusCode,
    DateOnly EffectiveFromDate,
    DateOnly? EffectiveToDate,
    int PolicyVersion);