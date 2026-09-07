using Microsoft.Extensions.Logging;
using PrivacyComply.Application.Abstractions.Audit;
using PrivacyComply.Application.Abstractions.Observability;
using PrivacyComply.Application.Abstractions.Security;
using PrivacyComply.Application.Abstractions.Tenancy;

namespace PrivacyComply.Infrastructure.Audit;

public sealed class SecurityAuditService : ISecurityAuditService
{
    private readonly ILogger<SecurityAuditService> _logger;
    private readonly IAuditEventWriter _auditEventWriter;
    private readonly ITenantContext _tenantContext;
    private readonly ICorrelationContext _correlationContext;
    private readonly IActorContext _actorContext;

    public SecurityAuditService(
        ILogger<SecurityAuditService> logger,
        IAuditEventWriter auditEventWriter,
        ITenantContext tenantContext,
        ICorrelationContext correlationContext,
        IActorContext actorContext)
    {
        _logger = logger;
        _auditEventWriter = auditEventWriter;
        _tenantContext = tenantContext;
        _correlationContext = correlationContext;
        _actorContext = actorContext;
    }

    public async Task RecordAsync(
        SecurityAuditEvent securityAuditEvent,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantContext.HasTenant)
        {
            throw new InvalidOperationException(
                "A tenant context is required to record a security audit event.");
        }

        if (!_actorContext.HasActor)
        {
            throw new InvalidOperationException(
                "A valid actor context is required to record a security audit event.");
        }
        if (string.IsNullOrWhiteSpace(_actorContext.ActorTypeCode))
        {
            throw new InvalidOperationException(
                "ActorTypeCode cannot be empty.");
        }

        if (_actorContext.ActorTypeCode.Length > 30)
        {
            throw new InvalidOperationException(
                "ActorTypeCode cannot exceed 30 characters.");
        }

        if (_actorContext.ActorReference?.Length > 150)
        {
            throw new InvalidOperationException(
                "ActorReference cannot exceed 150 characters.");
        }
        if (string.IsNullOrWhiteSpace(securityAuditEvent.EventTypeCode))
        {
            throw new ArgumentException(
                "EventTypeCode is required.",
                nameof(securityAuditEvent));
        }

        if (string.IsNullOrWhiteSpace(securityAuditEvent.EventCategoryCode))
        {
            throw new ArgumentException(
                "EventCategoryCode is required.",
                nameof(securityAuditEvent));
        }

        if (string.IsNullOrWhiteSpace(securityAuditEvent.ActionCode))
        {
            throw new ArgumentException(
                "ActionCode is required.",
                nameof(securityAuditEvent));
        }

        if (string.IsNullOrWhiteSpace(securityAuditEvent.EventStatusCode))
        {
            throw new ArgumentException(
                "EventStatusCode is required.",
                nameof(securityAuditEvent));
        }

        if (string.IsNullOrWhiteSpace(securityAuditEvent.EntityTypeCode))
        {
            throw new ArgumentException(
                "EntityTypeCode is required.",
                nameof(securityAuditEvent));
        }

        if (string.IsNullOrWhiteSpace(securityAuditEvent.EventSummary))
        {
            throw new ArgumentException(
                "EventSummary is required.",
                nameof(securityAuditEvent));
        }
        var correlationId =
            _correlationContext.HasCorrelationId
                ? _correlationContext.CorrelationId
                : Guid.NewGuid();

        if (securityAuditEvent.WriteSecurityLog)
        {
            _logger.LogWarning(
                "{SecurityEvent} Security audit event recorded. " +
                "CorrelationId={CorrelationId} " +
                "EventTypeCode={EventTypeCode} " +
                "EventCategoryCode={EventCategoryCode} " +
                "ActionCode={ActionCode} " +
                "EventStatusCode={EventStatusCode} " +
                "ActorTypeCode={ActorTypeCode} " +
                "ActorReference={ActorReference} " +
                "EntityTypeCode={EntityTypeCode} " +
                "EntityId={EntityId} " +
                "EntityReference={EntityReference} " +
                 "EventSummary={EventSummary}",
                true,
                correlationId,
                securityAuditEvent.EventTypeCode,
                securityAuditEvent.EventCategoryCode,
                securityAuditEvent.ActionCode,
                securityAuditEvent.EventStatusCode,
                _actorContext.ActorTypeCode,
                _actorContext.ActorReference,
                securityAuditEvent.EntityTypeCode,
                securityAuditEvent.EntityId,
                securityAuditEvent.EntityReference,
                securityAuditEvent.EventSummary);
        }

        if (securityAuditEvent.WriteImmutableAudit)
        {
            await _auditEventWriter.WriteAsync(
                new AuditEventWriteRequest(
                    OrganisationId: _tenantContext.OrganisationId,
                    EventTypeCode: securityAuditEvent.EventTypeCode,
                    EventCategoryCode: securityAuditEvent.EventCategoryCode,
                    ActionCode: securityAuditEvent.ActionCode,
                    EventStatusCode: securityAuditEvent.EventStatusCode,
                    ActorTypeCode: _actorContext.ActorTypeCode,
                    ActorReference: _actorContext.ActorReference,
                    EntityTypeCode: securityAuditEvent.EntityTypeCode,
                    EntityId: securityAuditEvent.EntityId,
                    EntityReference: securityAuditEvent.EntityReference,
                    CorrelationId: correlationId,
                    EvidenceRecordId: securityAuditEvent.EvidenceRecordId,
                    EventSummary: securityAuditEvent.EventSummary),
                cancellationToken);
        }
    }
}