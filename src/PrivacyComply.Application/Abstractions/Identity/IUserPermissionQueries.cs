namespace PrivacyComply.Application.Abstractions.Identity;

public interface IUserPermissionQueries
{
    Task<IReadOnlyList<UserPermissionRecord>>
        GetEffectivePermissionsAsync(
            Guid userAccountId,
            Guid organisationId,
            CancellationToken cancellationToken = default);
}

public sealed record UserPermissionRecord(
    Guid RoleId,
    string RoleCode,
    string RoleName,
    string RoleScopeCode,
    Guid PermissionId,
    string PermissionCode,
    string PermissionName);