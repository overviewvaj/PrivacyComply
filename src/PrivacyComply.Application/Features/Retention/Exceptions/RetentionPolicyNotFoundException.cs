namespace PrivacyComply.Application.Features.Retention.Exceptions;

public sealed class RetentionPolicyNotFoundException : Exception
{
    public RetentionPolicyNotFoundException(Guid retentionPolicyId)
        : base(
            $"Retention policy '{retentionPolicyId}' was not found for the current organisation.")
    {
        RetentionPolicyId = retentionPolicyId;
    }

    public Guid RetentionPolicyId { get; }
}