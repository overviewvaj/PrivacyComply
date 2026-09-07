namespace PrivacyComply.Application.Abstractions.Observability;

public interface ICorrelationContext
{
    Guid CorrelationId { get; }

    bool HasCorrelationId { get; }
}