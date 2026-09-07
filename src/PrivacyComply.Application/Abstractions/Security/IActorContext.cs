namespace PrivacyComply.Application.Abstractions.Security;

public interface IActorContext
{
    string ActorTypeCode { get; }

    string? ActorReference { get; }

    bool HasActor { get; }
}