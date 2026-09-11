namespace PrivacyComply.Application.Abstractions.Tenancy;

public interface IAuthenticatedTenantResolver
{
    Task<AuthenticatedTenantResolutionResult> ResolveAsync(
        Guid userAccountId,
        string organisationSlug,
        CancellationToken cancellationToken = default);
}

public sealed record AuthenticatedTenantResolutionResult(
    bool Succeeded,
    string ResultCode,
    Guid? OrganisationId,
    Guid? OrganisationMembershipId,
    string? OrganisationCode,
    string? OrganisationName,
    string? OrganisationSlug);