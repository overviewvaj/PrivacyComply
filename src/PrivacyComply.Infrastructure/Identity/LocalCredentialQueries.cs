using Microsoft.Data.SqlClient;
using PrivacyComply.Application.Abstractions.Identity;
using PrivacyComply.Infrastructure.Database;

namespace PrivacyComply.Infrastructure.Identity;

public sealed class LocalCredentialQueries
    : ILocalCredentialQueries
{
    private readonly SqlConnectionFactory _connectionFactory;

    public LocalCredentialQueries(
        SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<LocalCredentialRecord?> GetActiveAsync(
        Guid userAccountId,
        CancellationToken cancellationToken = default)
    {
        if (userAccountId == Guid.Empty)
        {
            throw new ArgumentException(
                "User account ID cannot be empty.",
                nameof(userAccountId));
        }

        const string sql = """
            SELECT TOP (1)
                lc.LocalCredentialId,
                lc.UserAccountId,
                lc.PasswordHash,
                lc.PasswordAlgorithmCode,
                lc.CredentialVersion,
                lc.MustChangePassword,
                lc.IsActive
            FROM iam.LocalCredential AS lc
            WHERE lc.UserAccountId = @UserAccountId
              AND lc.IsActive = 1;
            """;

        await using var connection =
            await _connectionFactory.OpenConnectionAsync(
                cancellationToken);

        await using var command =
            new SqlCommand(sql, connection);

        command.Parameters.Add(
            new SqlParameter(
                "@UserAccountId",
                System.Data.SqlDbType.UniqueIdentifier)
            {
                Value = userAccountId
            });

        await using var reader =
            await command.ExecuteReaderAsync(
                cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new LocalCredentialRecord(
            LocalCredentialId:
                reader.GetGuid(
                    reader.GetOrdinal("LocalCredentialId")),
            UserAccountId:
                reader.GetGuid(
                    reader.GetOrdinal("UserAccountId")),
            PasswordHash:
                reader.GetString(
                    reader.GetOrdinal("PasswordHash")),
            PasswordAlgorithmCode:
                reader.GetString(
                    reader.GetOrdinal("PasswordAlgorithmCode")),
            CredentialVersion:
                reader.GetInt32(
                    reader.GetOrdinal("CredentialVersion")),
            MustChangePassword:
                reader.GetBoolean(
                    reader.GetOrdinal("MustChangePassword")),
            IsActive:
                reader.GetBoolean(
                    reader.GetOrdinal("IsActive")));
    }
}