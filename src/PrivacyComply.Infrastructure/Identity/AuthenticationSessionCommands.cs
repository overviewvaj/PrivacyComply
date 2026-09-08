using System.Data;
using Microsoft.Data.SqlClient;
using PrivacyComply.Application.Abstractions.Identity;
using PrivacyComply.Infrastructure.Database;

namespace PrivacyComply.Infrastructure.Identity;

public sealed class AuthenticationSessionCommands
    : IAuthenticationSessionCommands
{
    private readonly SqlConnectionFactory _connectionFactory;

    public AuthenticationSessionCommands(
        SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AuthenticationSessionRecord> CreateAsync(
        CreateAuthenticationSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.UserAccountId == Guid.Empty)
        {
            throw new ArgumentException(
                "User account ID cannot be empty.",
                nameof(command));
        }

        if (string.IsNullOrWhiteSpace(command.SessionReference))
        {
            throw new ArgumentException(
                "Session reference cannot be empty.",
                nameof(command));
        }

        if (string.IsNullOrWhiteSpace(
                command.AuthenticationMethodCode))
        {
            throw new ArgumentException(
                "Authentication method code cannot be empty.",
                nameof(command));
        }

        if (command.ExpiresDateTime <= command.IssuedDateTime)
        {
            throw new ArgumentException(
                "Session expiry must be later than the issued date.",
                nameof(command));
        }

        const string sql = """
            INSERT INTO iam.AuthenticationSession
            (
                UserAccountId,
                SessionReference,
                SessionStatusCode,
                AuthenticationMethodCode,
                IssuedDateTime,
                ExpiresDateTime,
                LastActivityDateTime,
                ClientIpAddress,
                ClientUserAgent,
                CreatedDateTime,
                CreatedBy
            )
            OUTPUT
                INSERTED.AuthenticationSessionId
            VALUES
            (
                @UserAccountId,
                @SessionReference,
                N'ACTIVE',
                @AuthenticationMethodCode,
                @IssuedDateTime,
                @ExpiresDateTime,
                @LastActivityDateTime,
                @ClientIpAddress,
                @ClientUserAgent,
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
                "@SessionReference",
                SqlDbType.NVarChar,
                200)
            {
                Value = command.SessionReference
            });

        sqlCommand.Parameters.Add(
            new SqlParameter(
                "@AuthenticationMethodCode",
                SqlDbType.NVarChar,
                50)
            {
                Value = command.AuthenticationMethodCode
            });

        sqlCommand.Parameters.Add(
            new SqlParameter(
                "@IssuedDateTime",
                SqlDbType.DateTime2)
            {
                Value = command.IssuedDateTime
            });

        sqlCommand.Parameters.Add(
            new SqlParameter(
                "@ExpiresDateTime",
                SqlDbType.DateTime2)
            {
                Value = command.ExpiresDateTime
            });

        sqlCommand.Parameters.Add(
            new SqlParameter(
                "@LastActivityDateTime",
                SqlDbType.DateTime2)
            {
                Value = command.IssuedDateTime
            });

        sqlCommand.Parameters.Add(
            new SqlParameter(
                "@ClientIpAddress",
                SqlDbType.NVarChar,
                64)
            {
                Value =
                    string.IsNullOrWhiteSpace(
                        command.ClientIpAddress)
                        ? DBNull.Value
                        : command.ClientIpAddress
            });

        sqlCommand.Parameters.Add(
            new SqlParameter(
                "@ClientUserAgent",
                SqlDbType.NVarChar,
                1000)
            {
                Value =
                    string.IsNullOrWhiteSpace(
                        command.ClientUserAgent)
                        ? DBNull.Value
                        : command.ClientUserAgent
            });

        var authenticationSessionIdObject =
            await sqlCommand.ExecuteScalarAsync(
                cancellationToken);

        if (authenticationSessionIdObject is not Guid
            authenticationSessionId)
        {
            throw new InvalidOperationException(
                "Authentication session could not be created.");
        }

        return new AuthenticationSessionRecord(
            authenticationSessionId,
            command.UserAccountId,
            command.SessionReference,
            "ACTIVE",
            command.AuthenticationMethodCode,
            command.IssuedDateTime,
            command.ExpiresDateTime,
            command.IssuedDateTime,
            null,
            null);
    }

    public async Task RevokeAsync(
        Guid authenticationSessionId,
        string revokedReasonCode,
        DateTime revokedDateTime,
        CancellationToken cancellationToken = default)
    {
        if (authenticationSessionId == Guid.Empty)
        {
            throw new ArgumentException(
                "Authentication session ID cannot be empty.",
                nameof(authenticationSessionId));
        }

        if (string.IsNullOrWhiteSpace(revokedReasonCode))
        {
            throw new ArgumentException(
                "Revoked reason code cannot be empty.",
                nameof(revokedReasonCode));
        }

        const string sql = """
            UPDATE iam.AuthenticationSession
            SET
                SessionStatusCode = N'REVOKED',
                RevokedDateTime = @RevokedDateTime,
                RevokedReasonCode = @RevokedReasonCode,
                UpdatedDateTime = SYSUTCDATETIME(),
                UpdatedBy = N'PrivacyComply.Api'
            WHERE AuthenticationSessionId =
                    @AuthenticationSessionId
              AND SessionStatusCode = N'ACTIVE';
            """;

        await using var connection =
            await _connectionFactory.OpenConnectionAsync(
                cancellationToken);

        await using var sqlCommand =
            new SqlCommand(sql, connection);

        sqlCommand.Parameters.Add(
            new SqlParameter(
                "@AuthenticationSessionId",
                SqlDbType.UniqueIdentifier)
            {
                Value = authenticationSessionId
            });

        sqlCommand.Parameters.Add(
            new SqlParameter(
                "@RevokedDateTime",
                SqlDbType.DateTime2)
            {
                Value = revokedDateTime
            });

        sqlCommand.Parameters.Add(
            new SqlParameter(
                "@RevokedReasonCode",
                SqlDbType.NVarChar,
                100)
            {
                Value = revokedReasonCode
            });

        await sqlCommand.ExecuteNonQueryAsync(
            cancellationToken);
    }
}