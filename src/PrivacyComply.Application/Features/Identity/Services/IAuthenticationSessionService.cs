namespace PrivacyComply.Application.Features.Identity.Services;

public interface IAuthenticationSessionService
{
    Task<AuthenticationSessionResult> GetCurrentSessionAsync(
        AuthenticationSessionRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record AuthenticationSessionRequest(
    Guid UserAccountId,
    Guid AuthenticationSessionId,
    string SessionReference,
    string EmailAddress,
    string DisplayName,
    bool IsPlatformUser);

public sealed record AuthenticationSessionResult(
    bool IsAuthenticated,
    string ResultCode,
    string? Message,
    Guid? UserAccountId,
    string? DisplayName,
    bool IsPlatformUser,
    IReadOnlyList<AuthenticationSessionOrganisationResult> Organisations);

public sealed record AuthenticationSessionOrganisationResult(
    Guid OrganisationId,
    Guid OrganisationMembershipId,
    string OrganisationCode,
    string OrganisationName,
    string OrganisationSlug,
    bool IsPrimaryOrganisation,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);