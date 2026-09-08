using System.Data;
using Microsoft.Data.SqlClient;
using PrivacyComply.Application.Abstractions.Identity;
using PrivacyComply.Infrastructure.Database;

namespace PrivacyComply.Infrastructure.Identity;

public sealed class AuthenticationSessionQueries
    : IAuthenticationSessionQueries
{
    private readonly SqlConnectionFactory _connectionFactory;

    public AuthenticationSessionQueries(
        SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AuthenticationSessionRecord?>
        GetActiveByReferenceAsync(
            string sessionReference,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionReference))
        {
            return null;
        }

        const string sql = """
            SELECT TOP (1)
                AuthenticationSessionId,
                UserAccountId,
                SessionReference,
                SessionStatusCode,
                AuthenticationMethodCode,
                IssuedDateTime,
                ExpiresDateTime,
                LastActivityDateTime,
                RevokedDateTime,
                RevokedReasonCode
            FROM iam.AuthenticationSession
            WHERE SessionReference = @SessionReference
              AND SessionStatusCode = N'ACTIVE'
              AND RevokedDateTime IS NULL
              AND ExpiresDateTime > SYSUTCDATETIME();
            """;

        await using var connection =
            await _connectionFactory.OpenConnectionAsync(
                cancellationToken);

        await using var command =
            new SqlCommand(sql, connection);

        command.Parameters.Add(
            new SqlParameter(
                "@SessionReference",
                SqlDbType.NVarChar,
                200)
            {
                Value = sessionReference
            });

        await using var reader =
            await command.ExecuteReaderAsync(
                CommandBehavior.SingleRow,
                cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new AuthenticationSessionRecord(
            reader.GetGuid(
                reader.GetOrdinal(
                    "AuthenticationSessionId")),

            reader.GetGuid(
                reader.GetOrdinal(
                    "UserAccountId")),

            reader.GetString(
                reader.GetOrdinal(
                    "SessionReference")),

            reader.GetString(
                reader.GetOrdinal(
                    "SessionStatusCode")),

            reader.GetString(
                reader.GetOrdinal(
                    "AuthenticationMethodCode")),

            reader.GetDateTime(
                reader.GetOrdinal(
                    "IssuedDateTime")),

            reader.GetDateTime(
                reader.GetOrdinal(
                    "ExpiresDateTime")),

            reader.IsDBNull(
                reader.GetOrdinal(
                    "LastActivityDateTime"))
                ? null
                : reader.GetDateTime(
                    reader.GetOrdinal(
                        "LastActivityDateTime")),

            reader.IsDBNull(
                reader.GetOrdinal(
                    "RevokedDateTime"))
                ? null
                : reader.GetDateTime(
                    reader.GetOrdinal(
                        "RevokedDateTime")),

            reader.IsDBNull(
                reader.GetOrdinal(
                    "RevokedReasonCode"))
                ? null
                : reader.GetString(
                    reader.GetOrdinal(
                        "RevokedReasonCode")));
    }
}