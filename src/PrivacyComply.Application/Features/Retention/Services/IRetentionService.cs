namespace PrivacyComply.Application.Features.Retention.Services;

public interface IRetentionService
{
    Task RecordPolicyReviewAsync(
        Guid retentionPolicyId,
        CancellationToken cancellationToken = default);
}