namespace PrivacyComply.Application.Features.Retention.Queries;

public interface IRetentionCoverageQueries
{
    Task<IReadOnlyList<RetentionCoverageSummary>> GetAllAsync(
        CancellationToken cancellationToken = default);
}

public sealed record RetentionCoverageSummary(
    Guid ProcessingActivityId,
    string ProcessingActivityCode,
    string ProcessingActivityName,
    Guid? RetentionPolicyId,
    string? RetentionPolicyCode,
    string? RetentionPolicyName,
    bool HasRetentionPolicy);