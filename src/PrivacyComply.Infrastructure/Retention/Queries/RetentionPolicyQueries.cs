using PrivacyComply.Application.Features.Retention.Queries;
using PrivacyComply.Infrastructure.Database;

namespace PrivacyComply.Infrastructure.Retention.Queries;

public sealed class RetentionPolicyQueries : IRetentionPolicyQueries
{
    private readonly SqlConnectionFactory _connectionFactory;

    public RetentionPolicyQueries(
        SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<RetentionPolicySummary>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            await _connectionFactory.OpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT
                RetentionPolicyId,
                RetentionPolicyCode,
                RetentionPolicyName,
                RetentionPeriodValue,
                RetentionPeriodUnitCode,
                RetentionTriggerCode,
                ExpiryActionCode,
                ReviewRequired,
                StatusCode,
                EffectiveFromDate,
                EffectiveToDate,
                PolicyVersion
            FROM retention.RetentionPolicy
            WHERE IsDeleted = 0
            ORDER BY RetentionPolicyName;
            """;

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        var retentionPolicies =
            new List<RetentionPolicySummary>();

        while (await reader.ReadAsync(cancellationToken))
        {
            retentionPolicies.Add(
                new RetentionPolicySummary(
                    RetentionPolicyId: reader.GetGuid(0),
                    RetentionPolicyCode: reader.GetString(1),
                    RetentionPolicyName: reader.GetString(2),
                    RetentionPeriodValue: reader.GetInt32(3),
                    RetentionPeriodUnitCode: reader.GetString(4),
                    RetentionTriggerCode: reader.GetString(5),
                    ExpiryActionCode: reader.GetString(6),
                    ReviewRequired: reader.GetBoolean(7),
                    StatusCode: reader.GetString(8),
                    EffectiveFromDate: DateOnly.FromDateTime(
                        reader.GetDateTime(9)),
                    EffectiveToDate: reader.IsDBNull(10)
                        ? null
                        : DateOnly.FromDateTime(reader.GetDateTime(10)),
                    PolicyVersion: reader.GetInt32(11)
                ));
        }

        return retentionPolicies;
    }

    public async Task<bool> ExistsAsync(
        Guid retentionPolicyId,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            await _connectionFactory.OpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT CASE
                WHEN EXISTS
                (
                    SELECT 1
                    FROM retention.RetentionPolicy
                    WHERE RetentionPolicyId = @RetentionPolicyId
                      AND IsDeleted = 0
                )
                THEN CAST(1 AS bit)
                ELSE CAST(0 AS bit)
            END;
            """;

        command.Parameters.Add(
            "@RetentionPolicyId",
            System.Data.SqlDbType.UniqueIdentifier).Value =
            retentionPolicyId;

        var result =
            await command.ExecuteScalarAsync(cancellationToken);

        return result is bool exists && exists;
    }
}