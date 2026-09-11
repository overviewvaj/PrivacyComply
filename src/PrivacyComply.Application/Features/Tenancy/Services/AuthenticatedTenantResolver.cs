using PrivacyComply.Application.Abstractions.Identity;
using PrivacyComply.Application.Abstractions.Tenancy;

namespace PrivacyComply.Application.Features.Tenancy.Services;

public sealed class AuthenticatedTenantResolver
    : IAuthenticatedTenantResolver
{
    private readonly IOrganisationMembershipQueries
        _organisationMembershipQueries;

    public AuthenticatedTenantResolver(
        IOrganisationMembershipQueries organisationMembershipQueries)
    {
        _organisationMembershipQueries =
            organisationMembershipQueries;
    }

    public async Task<AuthenticatedTenantResolutionResult> ResolveAsync(
        Guid userAccountId,
        string organisationSlug,
        CancellationToken cancellationToken = default)
    {
        if (userAccountId == Guid.Empty)
        {
            return new AuthenticatedTenantResolutionResult(
                false,
                "INVALID_USER",
                null,
                null,
                null,
                null,
                null);
        }

        if (string.IsNullOrWhiteSpace(organisationSlug))
        {
            return new AuthenticatedTenantResolutionResult(
                false,
                "INVALID_ORGANISATION_SLUG",
                null,
                null,
                null,
                null,
                null);
        }

        var normalizedOrganisationSlug =
            organisationSlug.Trim();

        var memberships =
            await _organisationMembershipQueries
                .GetActiveMembershipsAsync(
                    userAccountId,
                    cancellationToken);

        var matchingMembership =
            memberships.FirstOrDefault(
                membership =>
                    string.Equals(
                        membership.OrganisationSlug,
                        normalizedOrganisationSlug,
                        StringComparison.OrdinalIgnoreCase));

        if (matchingMembership is null)
        {
            return new AuthenticatedTenantResolutionResult(
                false,
                "ORGANISATION_ACCESS_DENIED",
                null,
                null,
                null,
                null,
                normalizedOrganisationSlug);
        }

        return new AuthenticatedTenantResolutionResult(
            true,
            "SUCCESS",
            matchingMembership.OrganisationId,
            matchingMembership.OrganisationMembershipId,
            matchingMembership.OrganisationCode,
            matchingMembership.OrganisationName,
            matchingMembership.OrganisationSlug);
    }
}