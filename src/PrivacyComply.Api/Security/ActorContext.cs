using PrivacyComply.Application.Abstractions.Security;

using ActorReferences =
    PrivacyComply.Application.Abstractions.Security.ActorReference;

using ActorTypes =
    PrivacyComply.Application.Abstractions.Security.ActorTypeCode;

namespace PrivacyComply.Api.Security;

public sealed class ActorContext : IActorContext
{
    public string ActorTypeCode => ActorTypes.System;

    public string? ActorReference => ActorReferences.Api;

    public bool HasActor => true;
}