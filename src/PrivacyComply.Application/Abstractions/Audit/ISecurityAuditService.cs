namespace PrivacyComply.Application.Abstractions.Audit;

public interface ISecurityAuditService
{
    Task RecordAsync(
        SecurityAuditEvent securityAuditEvent,
        CancellationToken cancellationToken = default);
}

public sealed record SecurityAuditEvent(
    string EventTypeCode,
    string EventCategoryCode,
    string ActionCode,
    string EventStatusCode,
    string EntityTypeCode,
    Guid? EntityId,
    string? EntityReference,
    Guid? EvidenceRecordId,
    string EventSummary,
    bool WriteSecurityLog = true,
    bool WriteImmutableAudit = true);