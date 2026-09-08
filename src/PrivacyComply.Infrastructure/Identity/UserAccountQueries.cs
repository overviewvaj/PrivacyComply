using Microsoft.Data.SqlClient;
using PrivacyComply.Application.Abstractions.Identity;
using PrivacyComply.Infrastructure.Database;

namespace PrivacyComply.Infrastructure.Identity;

public sealed class UserAccountQueries
    : IUserAccountQueries
{
    private readonly SqlConnectionFactory _connectionFactory;

    public UserAccountQueries(
        SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<UserAccountRecord?> GetByNormalizedEmailAsync(
        string normalizedEmailAddress,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(normalizedEmailAddress))
        {
            throw new ArgumentException(
                "Normalized email address cannot be empty.",
                nameof(normalizedEmailAddress));
        }

        const string sql = """
            SELECT TOP (1)
                ua.UserAccountId,
                ua.UserAccountCode,
                ua.EmailAddress,
                ua.NormalizedEmailAddress,
                ua.DisplayName,
                ua.AccountStatusCode,
                ua.EmailConfirmed,
                ua.IsPlatformUser,
                ua.FailedSignInCount,
                ua.LockoutEndDateTime,
                ua.LastSuccessfulSignInDateTime,
                ua.LastFailedSignInDateTime,
                ua.PasswordChangedDateTime,
                ua.SecurityStamp,
                ua.IsDeleted
            FROM iam.UserAccount AS ua
            WHERE ua.NormalizedEmailAddress = @NormalizedEmailAddress
              AND ua.IsDeleted = 0;
            """;

        await using var connection =
            await _connectionFactory.OpenConnectionAsync(
                cancellationToken);

        await using var command =
            new SqlCommand(sql, connection);

        command.Parameters.Add(
            new SqlParameter(
                "@NormalizedEmailAddress",
                System.Data.SqlDbType.NVarChar,
                320)
            {
                Value = normalizedEmailAddress
            });

        await using var reader =
            await command.ExecuteReaderAsync(
                cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new UserAccountRecord(
            UserAccountId:
                reader.GetGuid(
                    reader.GetOrdinal("UserAccountId")),
            UserAccountCode:
                reader.GetString(
                    reader.GetOrdinal("UserAccountCode")),
            EmailAddress:
                reader.GetString(
                    reader.GetOrdinal("EmailAddress")),
            NormalizedEmailAddress:
                reader.GetString(
                    reader.GetOrdinal("NormalizedEmailAddress")),
            DisplayName:
                reader.GetString(
                    reader.GetOrdinal("DisplayName")),
            AccountStatusCode:
                reader.GetString(
                    reader.GetOrdinal("AccountStatusCode")),
            EmailConfirmed:
                reader.GetBoolean(
                    reader.GetOrdinal("EmailConfirmed")),
            IsPlatformUser:
                reader.GetBoolean(
                    reader.GetOrdinal("IsPlatformUser")),
            FailedSignInCount:
                reader.GetInt32(
                    reader.GetOrdinal("FailedSignInCount")),
            LockoutEndDateTime:
                reader.IsDBNull(
                    reader.GetOrdinal("LockoutEndDateTime"))
                    ? null
                    : reader.GetDateTime(
                        reader.GetOrdinal("LockoutEndDateTime")),
            LastSuccessfulSignInDateTime:
                reader.IsDBNull(
                    reader.GetOrdinal("LastSuccessfulSignInDateTime"))
                    ? null
                    : reader.GetDateTime(
                        reader.GetOrdinal("LastSuccessfulSignInDateTime")),
            LastFailedSignInDateTime:
                reader.IsDBNull(
                    reader.GetOrdinal("LastFailedSignInDateTime"))
                    ? null
                    : reader.GetDateTime(
                        reader.GetOrdinal("LastFailedSignInDateTime")),
            PasswordChangedDateTime:
                reader.IsDBNull(
                    reader.GetOrdinal("PasswordChangedDateTime"))
                    ? null
                    : reader.GetDateTime(
                        reader.GetOrdinal("PasswordChangedDateTime")),
            SecurityStamp:
                reader.GetGuid(
                    reader.GetOrdinal("SecurityStamp")),
            IsDeleted:
                reader.GetBoolean(
                    reader.GetOrdinal("IsDeleted")));
    }
}