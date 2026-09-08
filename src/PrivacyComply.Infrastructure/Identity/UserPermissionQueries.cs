using System.Data;
using Microsoft.Data.SqlClient;
using PrivacyComply.Application.Abstractions.Identity;
using PrivacyComply.Infrastructure.Database;

namespace PrivacyComply.Infrastructure.Identity;

public sealed class UserPermissionQueries
    : IUserPermissionQueries
{
    private readonly SqlConnectionFactory _connectionFactory;

    public UserPermissionQueries(
        SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<UserPermissionRecord>>
        GetEffectivePermissionsAsync(
            Guid userAccountId,
            Guid organisationId,
            CancellationToken cancellationToken = default)
    {
        if (userAccountId == Guid.Empty)
        {
            throw new ArgumentException(
                "User account ID cannot be empty.",
                nameof(userAccountId));
        }

        if (organisationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Organisation ID cannot be empty.",
                nameof(organisationId));
        }

        await using var connection =
            await _connectionFactory.OpenConnectionAsync(
                cancellationToken);

        await using var command =
            new SqlCommand(
                "iam.usp_GetEffectivePermissionsForAuthentication",
                connection)
            {
                CommandType = CommandType.StoredProcedure
            };

        command.Parameters.Add(
            new SqlParameter(
                "@UserAccountId",
                SqlDbType.UniqueIdentifier)
            {
                Value = userAccountId
            });

        command.Parameters.Add(
            new SqlParameter(
                "@OrganisationId",
                SqlDbType.UniqueIdentifier)
            {
                Value = organisationId
            });

        var permissions =
            new List<UserPermissionRecord>();

        await using var reader =
            await command.ExecuteReaderAsync(
                cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            permissions.Add(
                new UserPermissionRecord(
                    reader.GetGuid(
                        reader.GetOrdinal(
                            "RoleId")),

                    reader.GetString(
                        reader.GetOrdinal(
                            "RoleCode")),

                    reader.GetString(
                        reader.GetOrdinal(
                            "RoleName")),

                    reader.GetString(
                        reader.GetOrdinal(
                            "RoleScopeCode")),

                    reader.GetGuid(
                        reader.GetOrdinal(
                            "PermissionId")),

                    reader.GetString(
                        reader.GetOrdinal(
                            "PermissionCode")),

                    reader.GetString(
                        reader.GetOrdinal(
                            "PermissionName"))
                ));
        }

        return permissions;
    }
}