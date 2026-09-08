using Microsoft.Data.SqlClient;
using PrivacyComply.Application.Abstractions.Identity;
using PrivacyComply.Infrastructure.Database;

namespace PrivacyComply.Infrastructure.Identity;

public sealed class LoginPolicyQueries
    : ILoginPolicyQueries
{
    private readonly SqlConnectionFactory _connectionFactory;

    public LoginPolicyQueries(
        SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<LoginPolicyRecord?> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP (1)
                lp.LoginPolicyId,
                lp.PolicyCode,
                lp.PolicyName,
                lp.MinimumPasswordLength,
                lp.MaximumFailedSignInAttempts,
                lp.LockoutDurationMinutes,
                lp.SessionLifetimeMinutes,
                lp.PasswordHistoryCount,
                lp.PasswordMaximumAgeDays,
                lp.RequireMfa,
                lp.RequireMfaForPlatformUsers,
                lp.RequireEmailConfirmation
            FROM iam.LoginPolicy AS lp
            WHERE lp.IsActive = 1
            ORDER BY
                lp.CreatedDateTime DESC,
                lp.LoginPolicyId;
            """;

        await using var connection =
            await _connectionFactory.OpenConnectionAsync(
                cancellationToken);

        await using var command =
            new SqlCommand(sql, connection);

        await using var reader =
            await command.ExecuteReaderAsync(
                cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new LoginPolicyRecord(
            LoginPolicyId:
                reader.GetGuid(
                    reader.GetOrdinal("LoginPolicyId")),
            PolicyCode:
                reader.GetString(
                    reader.GetOrdinal("PolicyCode")),
            PolicyName:
                reader.GetString(
                    reader.GetOrdinal("PolicyName")),
            MinimumPasswordLength:
                reader.GetInt32(
                    reader.GetOrdinal("MinimumPasswordLength")),
            MaximumFailedSignInAttempts:
                reader.GetInt32(
                    reader.GetOrdinal("MaximumFailedSignInAttempts")),
            LockoutDurationMinutes:
                reader.GetInt32(
                    reader.GetOrdinal("LockoutDurationMinutes")),
            SessionLifetimeMinutes:
                reader.GetInt32(
                    reader.GetOrdinal("SessionLifetimeMinutes")),
            PasswordHistoryCount:
                reader.GetInt32(
                    reader.GetOrdinal("PasswordHistoryCount")),
            PasswordMaximumAgeDays:
                reader.IsDBNull(
                    reader.GetOrdinal("PasswordMaximumAgeDays"))
                    ? null
                    : reader.GetInt32(
                        reader.GetOrdinal("PasswordMaximumAgeDays")),
            RequireMfa:
                reader.GetBoolean(
                    reader.GetOrdinal("RequireMfa")),
            RequireMfaForPlatformUsers:
                reader.GetBoolean(
                    reader.GetOrdinal("RequireMfaForPlatformUsers")),
            RequireEmailConfirmation:
                reader.GetBoolean(
                    reader.GetOrdinal("RequireEmailConfirmation")));
    }
}