using Microsoft.Data.SqlClient;
using PrivacyComply.Application.Features.Runs.Commands;
using PrivacyComply.Infrastructure.Database;

namespace PrivacyComply.Infrastructure.Runs.Commands;

public sealed class AnalysisRunCommands
    : IAnalysisRunCommands
{
    private readonly SqlConnectionFactory _sqlConnectionFactory;

    public AnalysisRunCommands(
        SqlConnectionFactory sqlConnectionFactory)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
    }

    public async Task<CreateAnalysisRunResult> CreateAsync(
        CreateAnalysisRunCommand command,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            await _sqlConnectionFactory.OpenConnectionAsync(
                cancellationToken);

        const string sql = """
            DECLARE @AnalysisRunId UNIQUEIDENTIFIER = NEWID();
            DECLARE @RequestedDateTime DATETIME2(3) = SYSUTCDATETIME();

            DECLARE @NextRunNumber BIGINT;

            SELECT
                @NextRunNumber =
                    ISNULL(
                        MAX(
                            TRY_CONVERT(
                                BIGINT,
                                RIGHT(AnalysisRunCode, 6)
                            )
                        ),
                        0
                    ) + 1
            FROM workflow.AnalysisRun
            WHERE OrganisationId = @OrganisationId;

            DECLARE @AnalysisRunCode NVARCHAR(50) =
                CONCAT(
                    N'RUN-',
                    RIGHT(
                        CONCAT(
                            N'000000',
                            CONVERT(
                                NVARCHAR(20),
                                @NextRunNumber
                            )
                        ),
                        6
                    )
                );

            INSERT INTO workflow.AnalysisRun
            (
                AnalysisRunId,
                OrganisationId,
                AnalysisRunCode,
                RunStatusCode,
                SourceTypeCode,
                SourceName,
                SourceObjectName,
                RequestedByUserAccountId,
                RequestedDateTime,
                CorrelationId,
                CreatedDateTime,
                CreatedBy
            )
            VALUES
            (
                @AnalysisRunId,
                @OrganisationId,
                @AnalysisRunCode,
                N'QUEUED',
                @SourceTypeCode,
                @SourceName,
                @SourceObjectName,
                @RequestedByUserAccountId,
                @RequestedDateTime,
                @CorrelationId,
                @RequestedDateTime,
                CONVERT(
                    NVARCHAR(100),
                    @RequestedByUserAccountId
                )
            );

            SELECT
                @AnalysisRunId AS AnalysisRunId,
                @AnalysisRunCode AS AnalysisRunCode,
                N'QUEUED' AS RunStatusCode,
                @RequestedDateTime AS RequestedDateTime;
            """;

        await using var commandSql =
            new SqlCommand(sql, connection);

        commandSql.Parameters.AddWithValue(
            "@OrganisationId",
            command.OrganisationId);

        commandSql.Parameters.AddWithValue(
            "@SourceTypeCode",
            command.SourceTypeCode);

        commandSql.Parameters.AddWithValue(
            "@SourceName",
            command.SourceName);

        commandSql.Parameters.AddWithValue(
            "@SourceObjectName",
            (object?)command.SourceObjectName ??
            DBNull.Value);

        commandSql.Parameters.AddWithValue(
            "@RequestedByUserAccountId",
            command.RequestedByUserAccountId);

        commandSql.Parameters.AddWithValue(
            "@CorrelationId",
            (object?)command.CorrelationId ??
            DBNull.Value);

        await using var reader =
            await commandSql.ExecuteReaderAsync(
                cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException(
                "The analysis run was created but no result was returned.");
        }

        return new CreateAnalysisRunResult(
            reader.GetGuid(
                reader.GetOrdinal("AnalysisRunId")),
            reader.GetString(
                reader.GetOrdinal("AnalysisRunCode")),
            reader.GetString(
                reader.GetOrdinal("RunStatusCode")),
            reader.GetDateTime(
                reader.GetOrdinal("RequestedDateTime")));
    }
}