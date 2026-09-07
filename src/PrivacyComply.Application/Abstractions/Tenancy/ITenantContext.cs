namespace PrivacyComply.Application.Abstractions.Tenancy;

public interface ITenantContext
{
    Guid OrganisationId { get; }

    bool HasTenant { get; }
}