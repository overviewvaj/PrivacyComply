namespace PrivacyComply.Application.Abstractions.Audit;

public interface IAuditEventWriter
{
    Task WriteAsync(
        AuditEventWriteRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record AuditEventWriteRequest(
    Guid OrganisationId,
    string EventTypeCode,
    string EventCategoryCode,
    string ActionCode,
    string EventStatusCode,
    string ActorTypeCode,
    string? ActorReference,
    string EntityTypeCode,
    Guid? EntityId,
    string? EntityReference,
    Guid CorrelationId,
    Guid? EvidenceRecordId,
    string EventSummary);