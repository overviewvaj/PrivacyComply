namespace PrivacyComply.Application.Abstractions.Tenancy;

public interface ITenantContextSetter
{
    void SetOrganisation(Guid organisationId);
}