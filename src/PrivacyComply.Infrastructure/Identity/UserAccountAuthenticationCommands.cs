using Microsoft.Data.SqlClient;
using PrivacyComply.Application.Abstractions.Identity;
using PrivacyComply.Infrastructure.Database;

namespace PrivacyComply.Infrastructure.Identity;

public sealed class UserAccountAuthenticationCommands
    : IUserAccountAuthenticationCommands
{
    private readonly SqlConnectionFactory _connectionFactory;

    public UserAccountAuthenticationCommands(
        SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task RecordSuccessfulSignInAsync(
        Guid userAccountId,
        DateTime successfulSignInDateTime,
        CancellationToken cancellationToken = default)
    {
        if (userAccountId == Guid.Empty)
        {
            throw new ArgumentException(
                "User account ID cannot be empty.",
                nameof(userAccountId));
        }

        const string sql = """
            UPDATE iam.UserAccount
            SET
                FailedSignInCount = 0,
                LockoutEndDateTime = NULL,
                LastSuccessfulSignInDateTime = @SuccessfulSignInDateTime,
                UpdatedDateTime = SYSUTCDATETIME(),
                UpdatedBy = N'PrivacyComply.Api'
            WHERE UserAccountId = @UserAccountId
              AND IsDeleted = 0;
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

        command.Parameters.Add(
            new SqlParameter(
                "@SuccessfulSignInDateTime",
                System.Data.SqlDbType.DateTime2)
            {
                Value = successfulSignInDateTime
            });

        await command.ExecuteNonQueryAsync(
            cancellationToken);
    }

    public async Task RecordFailedSignInAsync(
        Guid userAccountId,
        int failedSignInCount,
        DateTime failedSignInDateTime,
        DateTime? lockoutEndDateTime,
        CancellationToken cancellationToken = default)
    {
        if (userAccountId == Guid.Empty)
        {
            throw new ArgumentException(
                "User account ID cannot be empty.",
                nameof(userAccountId));
        }

        if (failedSignInCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(failedSignInCount),
                "Failed sign-in count cannot be negative.");
        }

        const string sql = """
            UPDATE iam.UserAccount
            SET
                FailedSignInCount = @FailedSignInCount,
                LastFailedSignInDateTime = @FailedSignInDateTime,
                LockoutEndDateTime = @LockoutEndDateTime,
                UpdatedDateTime = SYSUTCDATETIME(),
                UpdatedBy = N'PrivacyComply.Api'
            WHERE UserAccountId = @UserAccountId
              AND IsDeleted = 0;
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

        command.Parameters.Add(
            new SqlParameter(
                "@FailedSignInCount",
                System.Data.SqlDbType.Int)
            {
                Value = failedSignInCount
            });

        command.Parameters.Add(
            new SqlParameter(
                "@FailedSignInDateTime",
                System.Data.SqlDbType.DateTime2)
            {
                Value = failedSignInDateTime
            });

        command.Parameters.Add(
            new SqlParameter(
                "@LockoutEndDateTime",
                System.Data.SqlDbType.DateTime2)
            {
                Value = lockoutEndDateTime is null
                    ? DBNull.Value
                    : lockoutEndDateTime.Value
            });

        await command.ExecuteNonQueryAsync(
            cancellationToken);
    }

    public async Task ClearExpiredLockoutAsync(
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
            UPDATE iam.UserAccount
            SET
                FailedSignInCount = 0,
                LockoutEndDateTime = NULL,
                UpdatedDateTime = SYSUTCDATETIME(),
                UpdatedBy = N'PrivacyComply.Api'
            WHERE UserAccountId = @UserAccountId
              AND LockoutEndDateTime IS NOT NULL
              AND LockoutEndDateTime <= SYSUTCDATETIME()
              AND IsDeleted = 0;
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

        await command.ExecuteNonQueryAsync(
            cancellationToken);
    }
}