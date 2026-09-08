using System.Data;
using Microsoft.Data.SqlClient;
using PrivacyComply.Application.Abstractions.Identity;
using PrivacyComply.Infrastructure.Database;

namespace PrivacyComply.Infrastructure.Identity;

public sealed class LocalCredentialCommands
    : ILocalCredentialCommands
{
    private readonly SqlConnectionFactory _connectionFactory;

    public LocalCredentialCommands(
        SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task CreateAsync(
        CreateLocalCredentialCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.UserAccountId == Guid.Empty)
        {
            throw new ArgumentException(
                "User account ID cannot be empty.",
                nameof(command));
        }

        if (string.IsNullOrWhiteSpace(command.PasswordHash))
        {
            throw new ArgumentException(
                "Password hash cannot be empty.",
                nameof(command));
        }

        if (string.IsNullOrWhiteSpace(
                command.PasswordAlgorithmCode))
        {
            throw new ArgumentException(
                "Password algorithm code cannot be empty.",
                nameof(command));
        }

        if (command.CredentialVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(command),
                "Credential version must be greater than zero.");
        }

        const string sql = """
            IF EXISTS
            (
                SELECT 1
                FROM iam.LocalCredential
                WHERE UserAccountId = @UserAccountId
                  AND IsActive = 1
            )
            BEGIN
                THROW 50011,
                    'An active local credential already exists for this user.',
                    1;
            END;

            INSERT INTO iam.LocalCredential
            (
                UserAccountId,
                PasswordHash,
                PasswordAlgorithmCode,
                CredentialVersion,
                MustChangePassword,
                IsActive,
                CreatedDateTime,
                CreatedBy
            )
            VALUES
            (
                @UserAccountId,
                @PasswordHash,
                @PasswordAlgorithmCode,
                @CredentialVersion,
                @MustChangePassword,
                1,
                SYSUTCDATETIME(),
                N'PrivacyComply.Api'
            );
            """;

        await using var connection =
            await _connectionFactory.OpenConnectionAsync(
                cancellationToken);

        await using var sqlCommand =
            new SqlCommand(sql, connection);

        sqlCommand.Parameters.Add(
            new SqlParameter(
                "@UserAccountId",
                SqlDbType.UniqueIdentifier)
            {
                Value = command.UserAccountId
            });

        sqlCommand.Parameters.Add(
            new SqlParameter(
                "@PasswordHash",
                SqlDbType.NVarChar,
                1000)
            {
                Value = command.PasswordHash
            });

        sqlCommand.Parameters.Add(
            new SqlParameter(
                "@PasswordAlgorithmCode",
                SqlDbType.NVarChar,
                50)
            {
                Value = command.PasswordAlgorithmCode
            });

        sqlCommand.Parameters.Add(
            new SqlParameter(
                "@CredentialVersion",
                SqlDbType.Int)
            {
                Value = command.CredentialVersion
            });

        sqlCommand.Parameters.Add(
            new SqlParameter(
                "@MustChangePassword",
                SqlDbType.Bit)
            {
                Value = command.MustChangePassword
            });

        await sqlCommand.ExecuteNonQueryAsync(
            cancellationToken);
    }
}