using Microsoft.Data.SqlClient;
using PrivacyComply.Application.Features.Runs.Commands;
using PrivacyComply.Infrastructure.Database;

namespace PrivacyComply.Infrastructure.Runs.Commands;

public sealed class CancelAnalysisRunCommand
    : ICancelAnalysisRunCommand
{
    private readonly SqlConnectionFactory _sqlConnectionFactory;

    public CancelAnalysisRunCommand(
        SqlConnectionFactory sqlConnectionFactory)
    {
        _sqlConnectionFactory =
            sqlConnectionFactory;
    }

    public async Task<CancelAnalysisRunResult> CancelAsync(
        CancelAnalysisRunRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            await _sqlConnectionFactory.OpenConnectionAsync(
                cancellationToken);

        const string sql = """
            UPDATE workflow.AnalysisRun
            SET
                RunStatusCode = 'CANCELLED',
                CancelledDateTime = SYSUTCDATETIME(),
                UpdatedDateTime = SYSUTCDATETIME(),
                UpdatedBy = @RequestedByUserAccountId
            OUTPUT
                inserted.AnalysisRunId,
                inserted.AnalysisRunCode,
                inserted.RunStatusCode
            WHERE
                OrganisationId = @OrganisationId
                AND AnalysisRunId = @AnalysisRunId
                AND RunStatusCode = 'QUEUED'
                AND IsDeleted = 0;
            """;

        await using var command =
            new SqlCommand(
                sql,
                connection);

        command.Parameters.AddWithValue(
            "@OrganisationId",
            request.OrganisationId);

        command.Parameters.AddWithValue(
            "@AnalysisRunId",
            request.AnalysisRunId);

        command.Parameters.AddWithValue(
            "@RequestedByUserAccountId",
            request.RequestedByUserAccountId);

        await using var reader =
            await command.ExecuteReaderAsync(
                cancellationToken);

        if (!await reader.ReadAsync(
                cancellationToken))
        {
            throw new InvalidOperationException(
                "The analysis run could not be cancelled. It may not exist or may no longer be queued.");
        }

        return new CancelAnalysisRunResult(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetString(2));
    }
}