using System.Data;
using Microsoft.Data.SqlClient;
using PrivacyComply.Application.Abstractions.Identity;
using PrivacyComply.Infrastructure.Database;

namespace PrivacyComply.Infrastructure.Identity;

public sealed class OrganisationMembershipQueries
    : IOrganisationMembershipQueries
{
    private readonly SqlConnectionFactory _connectionFactory;

    public OrganisationMembershipQueries(
        SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<OrganisationMembershipRecord>>
        GetActiveMembershipsAsync(
            Guid userAccountId,
            CancellationToken cancellationToken = default)
    {
        if (userAccountId == Guid.Empty)
        {
            throw new ArgumentException(
                "User account ID cannot be empty.",
                nameof(userAccountId));
        }

        await using var connection =
            await _connectionFactory.OpenConnectionAsync(
                cancellationToken);

        await using var command =
            new SqlCommand(
                "iam.usp_GetActiveMembershipsForAuthentication",
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

        var memberships =
            new List<OrganisationMembershipRecord>();

        await using var reader =
            await command.ExecuteReaderAsync(
                cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            memberships.Add(
                new OrganisationMembershipRecord(
                    reader.GetGuid(
                        reader.GetOrdinal(
                            "OrganisationMembershipId")),

                    reader.GetGuid(
                        reader.GetOrdinal(
                            "OrganisationId")),

                    reader.GetGuid(
                        reader.GetOrdinal(
                            "UserAccountId")),

                    reader.GetString(
                        reader.GetOrdinal(
                            "OrganisationCode")),

                    reader.GetString(
                        reader.GetOrdinal(
                            "OrganisationName")),

                    reader.GetString(
                        reader.GetOrdinal(
                            "OrganisationSlug")),

                    reader.GetString(
                        reader.GetOrdinal(
                            "MembershipStatusCode")),

                    reader.GetBoolean(
                        reader.GetOrdinal(
                            "IsPrimaryOrganisation")),

                    reader.GetDateTime(
                        reader.GetOrdinal(
                            "JoinedDateTime")),

                    reader.IsDBNull(
                        reader.GetOrdinal(
                            "LastAccessDateTime"))
                        ? null
                        : reader.GetDateTime(
                            reader.GetOrdinal(
                                "LastAccessDateTime"))
                ));
        }

        return memberships;
    }
}