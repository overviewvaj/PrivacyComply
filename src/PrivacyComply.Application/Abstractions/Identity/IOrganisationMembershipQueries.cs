namespace PrivacyComply.Application.Abstractions.Identity;

public interface IOrganisationMembershipQueries
{
    Task<IReadOnlyList<OrganisationMembershipRecord>>
        GetActiveMembershipsAsync(
            Guid userAccountId,
            CancellationToken cancellationToken = default);
}

public sealed record OrganisationMembershipRecord(
    Guid OrganisationMembershipId,
    Guid OrganisationId,
    Guid UserAccountId,
    string OrganisationCode,
    string OrganisationName,
    string OrganisationSlug,
    string MembershipStatusCode,
    bool IsPrimaryOrganisation,
    DateTime JoinedDateTime,
    DateTime? LastAccessDateTime);