using PrivacyComply.Application.Abstractions.Tenancy;

namespace PrivacyComply.Api.Tenancy;

public sealed class TenantContext : ITenantContext, ITenantContextSetter
{
    public Guid OrganisationId { get; private set; }

    public bool HasTenant => OrganisationId != Guid.Empty;

    public void SetOrganisation(Guid organisationId)
    {
        if (organisationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Organisation ID cannot be empty.",
                nameof(organisationId));
        }

        OrganisationId = organisationId;
    }
}