using PrivacyComply.Application.Abstractions.Audit;
using PrivacyComply.Application.Abstractions.Tenancy;
using PrivacyComply.Application.Features.Retention.Queries;
using PrivacyComply.Application.Features.Retention.Services;
using PrivacyComply.Application.Features.Retention.Exceptions;

namespace PrivacyComply.Infrastructure.Retention.Services;

public sealed class RetentionService : IRetentionService
{
    private readonly ISecurityAuditService _securityAuditService;
    private readonly ITenantContext _tenantContext;
    private readonly IRetentionPolicyQueries _retentionPolicyQueries;

    public RetentionService(
        ISecurityAuditService securityAuditService,
        ITenantContext tenantContext,
        IRetentionPolicyQueries retentionPolicyQueries)
    {
        _securityAuditService = securityAuditService;
        _tenantContext = tenantContext;
        _retentionPolicyQueries = retentionPolicyQueries;
    }

    public async Task RecordPolicyReviewAsync(
        Guid retentionPolicyId,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantContext.HasTenant)
        {
            throw new InvalidOperationException(
                "A valid organisation context is required.");
        }

        if (retentionPolicyId == Guid.Empty)
        {
            throw new ArgumentException(
                "Retention policy ID cannot be empty.",
                nameof(retentionPolicyId));
        }

        var exists = await _retentionPolicyQueries.ExistsAsync(
            retentionPolicyId,
            cancellationToken);

        if (!exists)
        {
            throw new RetentionPolicyNotFoundException(
                retentionPolicyId);
        }

        await _securityAuditService.RecordAsync(
            new SecurityAuditEvent(
                EventTypeCode: "RETENTION_POLICY_REVIEW",
                EventCategoryCode: SecurityAuditEventCategory.Retention,
                ActionCode: "REVIEW_RETENTION_POLICY",
                EventStatusCode: SecurityAuditEventStatus.Success,
                EntityTypeCode: "RETENTION_POLICY",
                EntityId: retentionPolicyId,
                EntityReference: null,
                EvidenceRecordId: null,
                EventSummary:
                    "Retention policy review recorded."),
            cancellationToken);
    }
}