namespace PrivacyComply.Application.Features.Identity.Services;

public interface ISignInService
{
    Task<SignInResult> SignInAsync(
        SignInRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record SignInRequest(
    string EmailAddress,
    string Password);

public sealed record SignInResult(
    bool Succeeded,
    string ResultCode,
    string? Message,
    Guid? UserAccountId,
    string? DisplayName,
    bool IsPlatformUser,
    bool RequiresMfa,
    bool MustChangePassword,
    string? AccessToken,
    string? TokenType,
    DateTime? ExpiresDateTime,
    IReadOnlyList<SignInOrganisationResult> Organisations);

public sealed record SignInOrganisationResult(
    Guid OrganisationId,
    Guid OrganisationMembershipId,
    string OrganisationCode,
    string OrganisationName,
    string OrganisationSlug,
    bool IsPrimaryOrganisation,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);