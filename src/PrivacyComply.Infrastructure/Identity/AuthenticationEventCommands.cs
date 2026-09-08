using Microsoft.Data.SqlClient;
using PrivacyComply.Application.Abstractions.Identity;
using PrivacyComply.Infrastructure.Database;

namespace PrivacyComply.Infrastructure.Identity;

public sealed class AuthenticationEventCommands
    : IAuthenticationEventCommands
{
    private readonly SqlConnectionFactory _connectionFactory;

    public AuthenticationEventCommands(
        SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task RecordAsync(
        AuthenticationEventRecord authenticationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(authenticationEvent);

        if (string.IsNullOrWhiteSpace(
                authenticationEvent.EventTypeCode))
        {
            throw new ArgumentException(
                "Authentication event type cannot be empty.",
                nameof(authenticationEvent));
        }

        if (string.IsNullOrWhiteSpace(
                authenticationEvent.EventStatusCode))
        {
            throw new ArgumentException(
                "Authentication event status cannot be empty.",
                nameof(authenticationEvent));
        }

        const string sql = """
            INSERT INTO iam.AuthenticationEvent
            (
                UserAccountId,
                AuthenticationSessionId,
                EventTypeCode,
                EventStatusCode,
                AuthenticationMethodCode,
                EmailAddressReference,
                FailureReasonCode,
                CorrelationId,
                ClientIpAddress,
                ClientUserAgent,
                EventDateTime,
                CreatedDateTime
            )
            VALUES
            (
                @UserAccountId,
                @AuthenticationSessionId,
                @EventTypeCode,
                @EventStatusCode,
                @AuthenticationMethodCode,
                @EmailAddressReference,
                @FailureReasonCode,
                @CorrelationId,
                @ClientIpAddress,
                @ClientUserAgent,
                @EventDateTime,
                SYSUTCDATETIME()
            );
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
                Value = authenticationEvent.UserAccountId
                    is null
                    ? DBNull.Value
                    : authenticationEvent.UserAccountId.Value
            });

        command.Parameters.Add(
            new SqlParameter(
                "@AuthenticationSessionId",
                System.Data.SqlDbType.UniqueIdentifier)
            {
                Value = authenticationEvent.AuthenticationSessionId
                    is null
                    ? DBNull.Value
                    : authenticationEvent.AuthenticationSessionId.Value
            });

        command.Parameters.Add(
            new SqlParameter(
                "@EventTypeCode",
                System.Data.SqlDbType.NVarChar,
                100)
            {
                Value = authenticationEvent.EventTypeCode
            });

        command.Parameters.Add(
            new SqlParameter(
                "@EventStatusCode",
                System.Data.SqlDbType.NVarChar,
                30)
            {
                Value = authenticationEvent.EventStatusCode
            });

        command.Parameters.Add(
            new SqlParameter(
                "@AuthenticationMethodCode",
                System.Data.SqlDbType.NVarChar,
                50)
            {
                Value = authenticationEvent.AuthenticationMethodCode
                    is null
                    ? DBNull.Value
                    : authenticationEvent.AuthenticationMethodCode
            });

        command.Parameters.Add(
            new SqlParameter(
                "@EmailAddressReference",
                System.Data.SqlDbType.NVarChar,
                320)
            {
                Value = authenticationEvent.EmailAddressReference
                    is null
                    ? DBNull.Value
                    : authenticationEvent.EmailAddressReference
            });

        command.Parameters.Add(
            new SqlParameter(
                "@FailureReasonCode",
                System.Data.SqlDbType.NVarChar,
                100)
            {
                Value = authenticationEvent.FailureReasonCode
                    is null
                    ? DBNull.Value
                    : authenticationEvent.FailureReasonCode
            });

        command.Parameters.Add(
            new SqlParameter(
                "@CorrelationId",
                System.Data.SqlDbType.UniqueIdentifier)
            {
                Value = authenticationEvent.CorrelationId
                    is null
                    ? DBNull.Value
                    : authenticationEvent.CorrelationId.Value
            });

        command.Parameters.Add(
            new SqlParameter(
                "@ClientIpAddress",
                System.Data.SqlDbType.NVarChar,
                64)
            {
                Value = authenticationEvent.ClientIpAddress
                    is null
                    ? DBNull.Value
                    : authenticationEvent.ClientIpAddress
            });

        command.Parameters.Add(
            new SqlParameter(
                "@ClientUserAgent",
                System.Data.SqlDbType.NVarChar,
                1000)
            {
                Value = authenticationEvent.ClientUserAgent
                    is null
                    ? DBNull.Value
                    : authenticationEvent.ClientUserAgent
            });

        command.Parameters.Add(
            new SqlParameter(
                "@EventDateTime",
                System.Data.SqlDbType.DateTime2)
            {
                Value = authenticationEvent.EventDateTime
            });

        await command.ExecuteNonQueryAsync(
            cancellationToken);
    }
}