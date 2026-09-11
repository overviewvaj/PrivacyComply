using Microsoft.Data.SqlClient;
using PrivacyComply.Application.Features.Runs.Queries;
using PrivacyComply.Infrastructure.Database;

namespace PrivacyComply.Infrastructure.Runs.Queries;

public sealed class AnalysisRunQueries
    : IAnalysisRunQueries
{
    private readonly SqlConnectionFactory _sqlConnectionFactory;

    public AnalysisRunQueries(
        SqlConnectionFactory sqlConnectionFactory)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
    }

    public async Task<IReadOnlyList<AnalysisRunSummary>>
        GetAllAsync(
            CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                AnalysisRunId,
                AnalysisRunCode,
                RunStatusCode,
                SourceTypeCode,
                SourceName,
                SourceObjectName,
                RequestedDateTime,
                StartedDateTime,
                CompletedDateTime,
                FailedDateTime,
                CancelledDateTime,
                TotalRecordsAnalysed,
                TotalFieldsDiscovered,
                TotalPersonalDataFields,
                TotalSensitiveDataFields,
                TotalUnclassifiedFields,
                ClassificationCoveragePercentage
            FROM workflow.AnalysisRun
            WHERE IsDeleted = 0
            ORDER BY RequestedDateTime DESC;
            """;

        await using var connection =
            await _sqlConnectionFactory.OpenConnectionAsync(
                cancellationToken);

        await using var command =
            new SqlCommand(
                sql,
                connection);

        await using var reader =
            await command.ExecuteReaderAsync(
                cancellationToken);

        var results =
            new List<AnalysisRunSummary>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(
                new AnalysisRunSummary(
                    reader.GetGuid(
                        reader.GetOrdinal("AnalysisRunId")),

                    reader.GetString(
                        reader.GetOrdinal("AnalysisRunCode")),

                    reader.GetString(
                        reader.GetOrdinal("RunStatusCode")),

                    reader.GetString(
                        reader.GetOrdinal("SourceTypeCode")),

                    reader.IsDBNull(
                        reader.GetOrdinal("SourceName"))
                        ? null
                        : reader.GetString(
                            reader.GetOrdinal("SourceName")),

                    reader.IsDBNull(
                        reader.GetOrdinal("SourceObjectName"))
                        ? null
                        : reader.GetString(
                            reader.GetOrdinal("SourceObjectName")),

                    reader.GetDateTime(
                        reader.GetOrdinal("RequestedDateTime")),

                    reader.IsDBNull(
                        reader.GetOrdinal("StartedDateTime"))
                        ? null
                        : reader.GetDateTime(
                            reader.GetOrdinal("StartedDateTime")),

                    reader.IsDBNull(
                        reader.GetOrdinal("CompletedDateTime"))
                        ? null
                        : reader.GetDateTime(
                            reader.GetOrdinal("CompletedDateTime")),

                    reader.IsDBNull(
                        reader.GetOrdinal("FailedDateTime"))
                        ? null
                        : reader.GetDateTime(
                            reader.GetOrdinal("FailedDateTime")),

                    reader.IsDBNull(
                        reader.GetOrdinal("CancelledDateTime"))
                        ? null
                        : reader.GetDateTime(
                            reader.GetOrdinal("CancelledDateTime")),

                    reader.IsDBNull(
                        reader.GetOrdinal("TotalRecordsAnalysed"))
                        ? null
                        : reader.GetInt64(
                            reader.GetOrdinal("TotalRecordsAnalysed")),

                    reader.IsDBNull(
                        reader.GetOrdinal("TotalFieldsDiscovered"))
                        ? null
                        : reader.GetInt32(
                            reader.GetOrdinal("TotalFieldsDiscovered")),

                    reader.IsDBNull(
                        reader.GetOrdinal("TotalPersonalDataFields"))
                        ? null
                        : reader.GetInt32(
                            reader.GetOrdinal("TotalPersonalDataFields")),

                    reader.IsDBNull(
                        reader.GetOrdinal("TotalSensitiveDataFields"))
                        ? null
                        : reader.GetInt32(
                            reader.GetOrdinal("TotalSensitiveDataFields")),

                    reader.IsDBNull(
                        reader.GetOrdinal("TotalUnclassifiedFields"))
                        ? null
                        : reader.GetInt32(
                            reader.GetOrdinal("TotalUnclassifiedFields")),

                    reader.IsDBNull(
                        reader.GetOrdinal(
                            "ClassificationCoveragePercentage"))
                        ? null
                        : reader.GetDecimal(
                            reader.GetOrdinal(
                                "ClassificationCoveragePercentage"))
                ));
        }

        return results;
    }
}