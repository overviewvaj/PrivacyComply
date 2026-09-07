using PrivacyComply.Application.Features.Retention.Queries;
using PrivacyComply.Infrastructure.Database;

namespace PrivacyComply.Infrastructure.Retention.Queries;

public sealed class RetentionCoverageQueries : IRetentionCoverageQueries
{
    private readonly SqlConnectionFactory _connectionFactory;

    public RetentionCoverageQueries(
        SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<RetentionCoverageSummary>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            await _connectionFactory.OpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT
                ProcessingActivityId,
                ProcessingActivityCode,
                ProcessingActivityName,
                RetentionPolicyId,
                RetentionPolicyCode,
                RetentionPolicyName,
                HasRetentionPolicy
            FROM retention.vw_RetentionCoverage
            ORDER BY ProcessingActivityName;
            """;

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        var coverage =
            new List<RetentionCoverageSummary>();

        while (await reader.ReadAsync(cancellationToken))
        {
            coverage.Add(
                new RetentionCoverageSummary(
                    ProcessingActivityId: reader.GetGuid(0),
                    ProcessingActivityCode: reader.GetString(1),
                    ProcessingActivityName: reader.GetString(2),

                    RetentionPolicyId: reader.IsDBNull(3)
                        ? null
                        : reader.GetGuid(3),

                    RetentionPolicyCode: reader.IsDBNull(4)
                        ? null
                        : reader.GetString(4),

                    RetentionPolicyName: reader.IsDBNull(5)
                        ? null
                        : reader.GetString(5),

                    HasRetentionPolicy:
                        reader.GetInt32(6) == 1
                ));
        }

        return coverage;
    }
}