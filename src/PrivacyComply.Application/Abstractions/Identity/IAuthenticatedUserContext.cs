namespace PrivacyComply.Application.Abstractions.Identity;

public interface IAuthenticatedUserContext
{
    bool IsAuthenticated { get; }

    Guid UserAccountId { get; }

    string? EmailAddress { get; }

    string? DisplayName { get; }

    bool IsPlatformUser { get; }
}