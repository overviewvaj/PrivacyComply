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

    public async Task<IReadOnlyList<DiscoveredFieldDto>>
        GetDiscoveredFieldsAsync(
            Guid organisationId,
            Guid analysisRunId,
            CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                DiscoveredFieldId,
                AnalysisRunId,
                SourceObjectName,
                FieldName,
                OrdinalPosition,
                InferredDataType,
                ClassificationStatus,
                ClassificationCode,
                ClassificationMethod,
                MatchPercentage,
                PrivacyCategory,
                IsPersonalData,
                IsRegulatedIdentifier,
                NonEmptyCount,
                EmptyCount,
                FindingCount,
                CreatedDateTime
            FROM workflow.DiscoveredField
            WHERE OrganisationId = @OrganisationId
              AND AnalysisRunId = @AnalysisRunId
            ORDER BY OrdinalPosition ASC;
            """;

        await using var connection =
            await _sqlConnectionFactory.OpenConnectionAsync(
                cancellationToken);

        await using var command =
            new SqlCommand(sql, connection);

        command.Parameters.AddWithValue("@OrganisationId", organisationId);
        command.Parameters.AddWithValue("@AnalysisRunId", analysisRunId);

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<DiscoveredFieldDto>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(
                new DiscoveredFieldDto(
                    reader.GetGuid(reader.GetOrdinal("DiscoveredFieldId")),
                    reader.GetGuid(reader.GetOrdinal("AnalysisRunId")),
                    reader.IsDBNull(reader.GetOrdinal("SourceObjectName"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("SourceObjectName")),
                    reader.GetString(reader.GetOrdinal("FieldName")),
                    reader.GetInt32(reader.GetOrdinal("OrdinalPosition")),
                    reader.GetString(reader.GetOrdinal("InferredDataType")),
                    reader.GetString(reader.GetOrdinal("ClassificationStatus")),
                    reader.IsDBNull(reader.GetOrdinal("ClassificationCode"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("ClassificationCode")),
                    reader.GetString(reader.GetOrdinal("ClassificationMethod")),
                    reader.IsDBNull(reader.GetOrdinal("MatchPercentage"))
                        ? null
                        : reader.GetDecimal(reader.GetOrdinal("MatchPercentage")),
                    reader.GetString(reader.GetOrdinal("PrivacyCategory")),
                    reader.GetBoolean(reader.GetOrdinal("IsPersonalData")),
                    reader.GetBoolean(reader.GetOrdinal("IsRegulatedIdentifier")),
                    reader.GetInt64(reader.GetOrdinal("NonEmptyCount")),
                    reader.GetInt64(reader.GetOrdinal("EmptyCount")),
                    reader.GetInt32(reader.GetOrdinal("FindingCount")),
                    reader.GetDateTime(reader.GetOrdinal("CreatedDateTime"))));
        }

        return results;
    }

    public async Task<IReadOnlyList<FindingDto>>
        GetFindingsAsync(
            Guid organisationId,
            Guid analysisRunId,
            CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                FindingId,
                AnalysisRunId,
                DiscoveredFieldId,
                FindingCode,
                FindingCategory,
                Severity,
                FieldName,
                RuleReference,
                FindingStatus,
                DetectedDateTime,
                ResolvedDateTime,
                Message,
                SafeMetadataJson,
                CreatedDateTime
            FROM workflow.Finding
            WHERE OrganisationId = @OrganisationId
              AND AnalysisRunId = @AnalysisRunId
            ORDER BY 
                CASE Severity
                    WHEN N'CRITICAL' THEN 1
                    WHEN N'ERROR' THEN 2
                    WHEN N'WARNING' THEN 3
                    WHEN N'INFO' THEN 4
                    ELSE 5
                END ASC,
                DetectedDateTime DESC;
            """;

        await using var connection =
            await _sqlConnectionFactory.OpenConnectionAsync(
                cancellationToken);

        await using var command =
            new SqlCommand(sql, connection);

        command.Parameters.AddWithValue("@OrganisationId", organisationId);
        command.Parameters.AddWithValue("@AnalysisRunId", analysisRunId);

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<FindingDto>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(
                new FindingDto(
                    reader.GetGuid(reader.GetOrdinal("FindingId")),
                    reader.GetGuid(reader.GetOrdinal("AnalysisRunId")),
                    reader.IsDBNull(reader.GetOrdinal("DiscoveredFieldId"))
                        ? null
                        : reader.GetGuid(reader.GetOrdinal("DiscoveredFieldId")),
                    reader.GetString(reader.GetOrdinal("FindingCode")),
                    reader.GetString(reader.GetOrdinal("FindingCategory")),
                    reader.GetString(reader.GetOrdinal("Severity")),
                    reader.IsDBNull(reader.GetOrdinal("FieldName"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("FieldName")),
                    reader.IsDBNull(reader.GetOrdinal("RuleReference"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("RuleReference")),
                    reader.GetString(reader.GetOrdinal("FindingStatus")),
                    reader.GetDateTime(reader.GetOrdinal("DetectedDateTime")),
                    reader.IsDBNull(reader.GetOrdinal("ResolvedDateTime"))
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("ResolvedDateTime")),
                    reader.IsDBNull(reader.GetOrdinal("Message"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("Message")),
                    reader.IsDBNull(reader.GetOrdinal("SafeMetadataJson"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("SafeMetadataJson")),
                    reader.GetDateTime(reader.GetOrdinal("CreatedDateTime"))));
        }

        return results;
    }


    public async Task<IReadOnlyList<EvidenceDto>>
        GetEvidenceAsync(
            Guid organisationId,
            Guid analysisRunId,
            CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                EvidenceId,
                OrganisationId,
                AnalysisRunId,
                DiscoveredFieldId,
                EvidenceReference,
                EvidenceType,
                SourceType,
                SourceReference,
                RuleVersion,
                AgentVersion,
                EvidenceHash,
                HashAlgorithmCode,
                GeneratedDateTime,
                CapturedBy,
                IsVerified,
                VerificationStatusCode,
                MetadataJson,
                FieldName,
                SourceObjectName,
                ClassificationCode,
                PrivacyCategory,
                IsPersonalData,
                IsRegulatedIdentifier
            FROM workflow.vw_RunEvidence
            WHERE OrganisationId = @OrganisationId
              AND AnalysisRunId = @AnalysisRunId
            ORDER BY 
                CASE EvidenceType
                    WHEN N'ANALYSIS_RUN' THEN 1
                    WHEN N'FIELD_CLASSIFICATION' THEN 2
                    ELSE 3
                END ASC,
                GeneratedDateTime ASC;
            """;

        await using var connection =
            await _sqlConnectionFactory.OpenConnectionAsync(
                cancellationToken);

        await using var command =
            new SqlCommand(sql, connection);

        command.Parameters.AddWithValue("@OrganisationId", organisationId);
        command.Parameters.AddWithValue("@AnalysisRunId", analysisRunId);

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<EvidenceDto>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(
                new EvidenceDto(
                    reader.GetGuid(reader.GetOrdinal("EvidenceId")),
                    reader.GetGuid(reader.GetOrdinal("OrganisationId")),
                    reader.GetGuid(reader.GetOrdinal("AnalysisRunId")),
                    reader.IsDBNull(reader.GetOrdinal("DiscoveredFieldId"))
                        ? null
                        : reader.GetGuid(reader.GetOrdinal("DiscoveredFieldId")),
                    reader.GetString(reader.GetOrdinal("EvidenceReference")),
                    reader.GetString(reader.GetOrdinal("EvidenceType")),
                    reader.GetString(reader.GetOrdinal("SourceType")),
                    reader.IsDBNull(reader.GetOrdinal("SourceReference"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("SourceReference")),
                    reader.IsDBNull(reader.GetOrdinal("RuleVersion"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("RuleVersion")),
                    reader.IsDBNull(reader.GetOrdinal("AgentVersion"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("AgentVersion")),
                    reader.GetString(reader.GetOrdinal("EvidenceHash")),
                    reader.GetString(reader.GetOrdinal("HashAlgorithmCode")),
                    reader.GetDateTime(reader.GetOrdinal("GeneratedDateTime")),
                    reader.IsDBNull(reader.GetOrdinal("CapturedBy"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("CapturedBy")),
                    reader.GetBoolean(reader.GetOrdinal("IsVerified")),
                    reader.GetString(reader.GetOrdinal("VerificationStatusCode")),
                    reader.IsDBNull(reader.GetOrdinal("MetadataJson"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("MetadataJson")),
                    reader.IsDBNull(reader.GetOrdinal("FieldName"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("FieldName")),
                    reader.IsDBNull(reader.GetOrdinal("SourceObjectName"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("SourceObjectName")),
                    reader.IsDBNull(reader.GetOrdinal("ClassificationCode"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("ClassificationCode")),
                    reader.IsDBNull(reader.GetOrdinal("PrivacyCategory"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("PrivacyCategory")),
                    reader.IsDBNull(reader.GetOrdinal("IsPersonalData"))
                        ? null
                        : reader.GetBoolean(reader.GetOrdinal("IsPersonalData")),
                    reader.IsDBNull(reader.GetOrdinal("IsRegulatedIdentifier"))
                        ? null
                        : reader.GetBoolean(reader.GetOrdinal("IsRegulatedIdentifier"))));
        }

        return results;
    }

    public async Task<IReadOnlyList<RuleEvaluationDto>>
        GetRuleEvaluationsAsync(
            Guid organisationId,
            Guid analysisRunId,
            CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                RuleEvaluationId,
                OrganisationId,
                AnalysisRunId,
                AnalysisRunCode,
                FrameworkCode,
                FrameworkVersion,
                RuleCode,
                RuleVersion,
                RuleName,
                RegulatoryReference,
                EvaluationOutcome,
                Severity,
                SummaryMessage,
                EvaluatedFieldsCount,
                FlaggedFieldsCount,
                FlaggedFieldNamesJson,
                EvaluatedDateTime,
                CreatedDateTime,
                CreatedBy
            FROM workflow.vw_RunRuleEvaluations
            WHERE
                OrganisationId = @OrganisationId
                AND AnalysisRunId = @AnalysisRunId
            ORDER BY RuleCode ASC;
            """;

        await using var connection =
            await _sqlConnectionFactory.OpenConnectionAsync(
                cancellationToken);

        await using var command =
            new SqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@OrganisationId",
            organisationId);

        command.Parameters.AddWithValue(
            "@AnalysisRunId",
            analysisRunId);

        await using var reader =
            await command.ExecuteReaderAsync(
                cancellationToken);

        var evaluations = new List<RuleEvaluationDto>();

        while (await reader.ReadAsync(cancellationToken))
        {
            evaluations.Add(new RuleEvaluationDto(
                reader.GetGuid(reader.GetOrdinal("RuleEvaluationId")),
                reader.GetGuid(reader.GetOrdinal("OrganisationId")),
                reader.GetGuid(reader.GetOrdinal("AnalysisRunId")),
                reader.IsDBNull(reader.GetOrdinal("AnalysisRunCode"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("AnalysisRunCode")),
                reader.GetString(reader.GetOrdinal("FrameworkCode")),
                reader.GetString(reader.GetOrdinal("FrameworkVersion")),
                reader.GetString(reader.GetOrdinal("RuleCode")),
                reader.GetString(reader.GetOrdinal("RuleVersion")),
                reader.GetString(reader.GetOrdinal("RuleName")),
                reader.IsDBNull(reader.GetOrdinal("RegulatoryReference"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("RegulatoryReference")),
                reader.GetString(reader.GetOrdinal("EvaluationOutcome")),
                reader.GetString(reader.GetOrdinal("Severity")),
                reader.GetString(reader.GetOrdinal("SummaryMessage")),
                reader.GetInt32(reader.GetOrdinal("EvaluatedFieldsCount")),
                reader.GetInt32(reader.GetOrdinal("FlaggedFieldsCount")),
                reader.IsDBNull(reader.GetOrdinal("FlaggedFieldNamesJson"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("FlaggedFieldNamesJson")),
                reader.GetDateTime(reader.GetOrdinal("EvaluatedDateTime")),
                reader.GetDateTime(reader.GetOrdinal("CreatedDateTime")),
                reader.GetString(reader.GetOrdinal("CreatedBy"))));
        }

        return evaluations;
    }

}
