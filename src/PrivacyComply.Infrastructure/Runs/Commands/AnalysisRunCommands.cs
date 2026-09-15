using PrivacyComply.Application.Features.Rules;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using PrivacyComply.Application.Features.Runs.Commands;
using PrivacyComply.Infrastructure.Database;

namespace PrivacyComply.Infrastructure.Runs.Commands;

public sealed class AnalysisRunCommands
    : IAnalysisRunCommands
{
    private readonly SqlConnectionFactory _sqlConnectionFactory;
    private readonly IDpdpRulesEngine _dpdpRulesEngine;

    public AnalysisRunCommands(
        SqlConnectionFactory sqlConnectionFactory,
        IDpdpRulesEngine dpdpRulesEngine)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
        _dpdpRulesEngine = dpdpRulesEngine;
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
                            CONVERT(NVARCHAR(20), @NextRunNumber)
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
                RequestedDateTime,
                CreatedDateTime,
                CreatedBy,
                IsDeleted,
                CorrelationId
            )
            VALUES
            (
                @AnalysisRunId,
                @OrganisationId,
                @AnalysisRunCode,
                N'QUEUED',
                @SourceTypeCode,
                @SourceName,
                @RequestedDateTime,
                @RequestedDateTime,
                CONVERT(
                    NVARCHAR(100),
                    @RequestedByUserAccountId
                ),
                0,
                @CorrelationId
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

    public async Task StartAsync(
        StartAnalysisRunCommand command,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            await _sqlConnectionFactory.OpenConnectionAsync(
                cancellationToken);

        const string sql = """
            UPDATE workflow.AnalysisRun
            SET
                RunStatusCode = N'RUNNING',
                StartedDateTime = SYSUTCDATETIME(),
                UpdatedDateTime = SYSUTCDATETIME(),
                UpdatedBy = CONVERT(
                    NVARCHAR(100),
                    @UserAccountId
                )
            WHERE
                OrganisationId = @OrganisationId
                AND AnalysisRunId = @AnalysisRunId
                AND RunStatusCode = N'QUEUED'
                AND IsDeleted = 0;
            """;

        await using var commandSql =
            new SqlCommand(sql, connection);

        commandSql.Parameters.AddWithValue(
            "@OrganisationId",
            command.OrganisationId);

        commandSql.Parameters.AddWithValue(
            "@AnalysisRunId",
            command.AnalysisRunId);

        commandSql.Parameters.AddWithValue(
            "@UserAccountId",
            command.UserAccountId);

        var affectedRows =
            await commandSql.ExecuteNonQueryAsync(
                cancellationToken);

        if (affectedRows != 1)
        {
            throw new InvalidOperationException(
                "The analysis run could not be started. It may not exist or may no longer be queued.");
        }
    }

    public async Task CompleteAsync(
        CompleteAnalysisRunCommand command,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            await _sqlConnectionFactory.OpenConnectionAsync(
                cancellationToken);

        await using var transaction =
            (SqlTransaction)await connection.BeginTransactionAsync(
                cancellationToken);

        try
        {
            const string updateRunSql = """
                UPDATE workflow.AnalysisRun
                SET
                    RunStatusCode = N'COMPLETED',
                    SourceObjectName = @SourceObjectName,
                    TotalRecordsAnalysed = @TotalRecordsAnalysed,
                    TotalFieldsDiscovered = @TotalFieldsDiscovered,
                    TotalPersonalDataFields = @TotalPersonalDataFields,
                    TotalUnclassifiedFields = @TotalUnclassifiedFields,
                    ClassificationCoveragePercentage =
                        @ClassificationCoveragePercentage,
                    CompletedDateTime = SYSUTCDATETIME(),
                    UpdatedDateTime = SYSUTCDATETIME(),
                    UpdatedBy = CONVERT(
                        NVARCHAR(100),
                        @UserAccountId
                    )
                WHERE
                    OrganisationId = @OrganisationId
                    AND AnalysisRunId = @AnalysisRunId
                    AND RunStatusCode = N'RUNNING'
                    AND IsDeleted = 0;
                """;

            await using var updateCommand =
                new SqlCommand(updateRunSql, connection, transaction);

            updateCommand.Parameters.AddWithValue(
                "@OrganisationId",
                command.OrganisationId);

            updateCommand.Parameters.AddWithValue(
                "@AnalysisRunId",
                command.AnalysisRunId);

            updateCommand.Parameters.AddWithValue(
                "@UserAccountId",
                command.UserAccountId);

            updateCommand.Parameters.AddWithValue(
                "@SourceObjectName",
                (object?)command.SourceObjectName ??
                DBNull.Value);

            updateCommand.Parameters.AddWithValue(
                "@TotalRecordsAnalysed",
                command.TotalRecordsAnalysed);

            updateCommand.Parameters.AddWithValue(
                "@TotalFieldsDiscovered",
                command.TotalFieldsDiscovered);

            updateCommand.Parameters.AddWithValue(
                "@TotalPersonalDataFields",
                command.TotalPersonalDataFields);

            updateCommand.Parameters.AddWithValue(
                "@TotalUnclassifiedFields",
                command.TotalUnclassifiedFields);

            updateCommand.Parameters.AddWithValue(
                "@ClassificationCoveragePercentage",
                command.ClassificationCoveragePercentage);

            var affectedRows =
                await updateCommand.ExecuteNonQueryAsync(
                    cancellationToken);

            if (affectedRows != 1)
            {
                throw new InvalidOperationException(
                    "The analysis run could not be completed. It may not exist or may no longer be running.");
            }

            var fieldIdMap = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

            if (command.DiscoveredFields is { Count: > 0 })
            {
                const string deleteExistingFieldsSql = """
                    DELETE FROM workflow.DiscoveredField
                    WHERE OrganisationId = @OrganisationId
                      AND AnalysisRunId = @AnalysisRunId;
                    """;

                await using var deleteFieldsCommand =
                    new SqlCommand(deleteExistingFieldsSql, connection, transaction);

                deleteFieldsCommand.Parameters.AddWithValue("@OrganisationId", command.OrganisationId);
                deleteFieldsCommand.Parameters.AddWithValue("@AnalysisRunId", command.AnalysisRunId);
                await deleteFieldsCommand.ExecuteNonQueryAsync(cancellationToken);

                const string insertFieldSql = """
                    INSERT INTO workflow.DiscoveredField
                    (
                        DiscoveredFieldId,
                        OrganisationId,
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
                        CreatedDateTime,
                        CreatedBy
                    )
                    VALUES
                    (
                        @DiscoveredFieldId,
                        @OrganisationId,
                        @AnalysisRunId,
                        @SourceObjectName,
                        @FieldName,
                        @OrdinalPosition,
                        @InferredDataType,
                        @ClassificationStatus,
                        @ClassificationCode,
                        @ClassificationMethod,
                        @MatchPercentage,
                        @PrivacyCategory,
                        @IsPersonalData,
                        @IsRegulatedIdentifier,
                        @NonEmptyCount,
                        @EmptyCount,
                        0,
                        SYSUTCDATETIME(),
                        CONVERT(NVARCHAR(100), @UserAccountId)
                    );
                    """;

                foreach (var field in command.DiscoveredFields)
                {
                    var fieldId = Guid.NewGuid();
                    fieldIdMap[field.FieldName] = fieldId;

                    await using var insertCommand =
                        new SqlCommand(insertFieldSql, connection, transaction);

                    insertCommand.Parameters.AddWithValue("@DiscoveredFieldId", fieldId);
                    insertCommand.Parameters.AddWithValue("@OrganisationId", command.OrganisationId);
                    insertCommand.Parameters.AddWithValue("@AnalysisRunId", command.AnalysisRunId);
                    insertCommand.Parameters.AddWithValue("@SourceObjectName", (object?)field.SourceObjectName ?? DBNull.Value);
                    insertCommand.Parameters.AddWithValue("@FieldName", field.FieldName);
                    insertCommand.Parameters.AddWithValue("@OrdinalPosition", field.OrdinalPosition);
                    insertCommand.Parameters.AddWithValue("@InferredDataType", field.InferredDataType);
                    insertCommand.Parameters.AddWithValue("@ClassificationStatus", field.ClassificationStatus);
                    insertCommand.Parameters.AddWithValue("@ClassificationCode", (object?)field.ClassificationCode ?? DBNull.Value);
                    insertCommand.Parameters.AddWithValue("@ClassificationMethod", field.ClassificationMethod);
                    insertCommand.Parameters.AddWithValue("@MatchPercentage", (object?)field.MatchPercentage ?? DBNull.Value);
                    insertCommand.Parameters.AddWithValue("@PrivacyCategory", field.PrivacyCategory);
                    insertCommand.Parameters.AddWithValue("@IsPersonalData", field.IsPersonalData);
                    insertCommand.Parameters.AddWithValue("@IsRegulatedIdentifier", field.IsRegulatedIdentifier);
                    insertCommand.Parameters.AddWithValue("@NonEmptyCount", field.NonEmptyCount);
                    insertCommand.Parameters.AddWithValue("@EmptyCount", field.EmptyCount);
                    insertCommand.Parameters.AddWithValue("@UserAccountId", command.UserAccountId);

                    await insertCommand.ExecuteNonQueryAsync(cancellationToken);
                }
            }

            // Evidence Persistence & Mapping
            var fieldEvidenceMap = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

            const string deleteExistingEvidenceSql = """
                DELETE FROM evidence.EvidenceRecord
                WHERE OrganisationId = @OrganisationId
                  AND AnalysisRunId = @AnalysisRunId;
                """;

            await using var deleteEvidenceCommand =
                new SqlCommand(deleteExistingEvidenceSql, connection, transaction);

            deleteEvidenceCommand.Parameters.AddWithValue("@OrganisationId", command.OrganisationId);
            deleteEvidenceCommand.Parameters.AddWithValue("@AnalysisRunId", command.AnalysisRunId);
            await deleteEvidenceCommand.ExecuteNonQueryAsync(cancellationToken);

            const string insertEvidenceSql = """
                INSERT INTO evidence.EvidenceRecord
                (
                    EvidenceRecordId,
                    OrganisationId,
                    AnalysisRunId,
                    DiscoveredFieldId,
                    EvidenceReference,
                    EvidenceTypeCode,
                    SourceTypeCode,
                    SourceReference,
                    RuleVersion,
                    AgentVersion,
                    EvidenceHash,
                    HashAlgorithmCode,
                    StorageReference,
                    CapturedDateTime,
                    CapturedBy,
                    IsVerified,
                    VerifiedDateTime,
                    VerifiedBy,
                    VerificationStatusCode,
                    MetadataJson,
                    CreatedDateTime
                )
                VALUES
                (
                    @EvidenceRecordId,
                    @OrganisationId,
                    @AnalysisRunId,
                    @DiscoveredFieldId,
                    @EvidenceReference,
                    @EvidenceTypeCode,
                    @SourceTypeCode,
                    @SourceReference,
                    @RuleVersion,
                    @AgentVersion,
                    @EvidenceHash,
                    @HashAlgorithmCode,
                    NULL,
                    SYSUTCDATETIME(),
                    CONVERT(NVARCHAR(150), @UserAccountId),
                    1,
                    SYSUTCDATETIME(),
                    N'PrivacyComply.ControlPlane',
                    N'VERIFIED',
                    @MetadataJson,
                    SYSUTCDATETIME()
                );
                """;

            if (command.Evidence is { Count: > 0 })
            {
                foreach (var ev in command.Evidence)
                {
                    var evidenceId = Guid.NewGuid();
                    Guid? discoveredFieldId = null;

                    if (!string.IsNullOrEmpty(ev.FieldName) && fieldIdMap.TryGetValue(ev.FieldName, out var matchedFieldId))
                    {
                        discoveredFieldId = matchedFieldId;
                        fieldEvidenceMap[ev.FieldName] = evidenceId;
                    }

                    await using var insertEvCmd =
                        new SqlCommand(insertEvidenceSql, connection, transaction);

                    var runPrefix = command.AnalysisRunId.ToString()[..8].ToUpperInvariant();
                    var evidenceRef = ev.EvidenceReference;
                    if (!evidenceRef.Contains(runPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        var baseName = evidenceRef.StartsWith("EVD-", StringComparison.OrdinalIgnoreCase)
                            ? evidenceRef[4..]
                            : evidenceRef;
                        evidenceRef = $"EVD-{runPrefix}-{baseName}";
                    }
                    if (evidenceRef.Length > 150)
                    {
                        evidenceRef = evidenceRef[..150];
                    }

                    insertEvCmd.Parameters.AddWithValue("@EvidenceRecordId", evidenceId);
                    insertEvCmd.Parameters.AddWithValue("@OrganisationId", command.OrganisationId);
                    insertEvCmd.Parameters.AddWithValue("@AnalysisRunId", command.AnalysisRunId);
                    insertEvCmd.Parameters.AddWithValue("@DiscoveredFieldId", (object?)discoveredFieldId ?? DBNull.Value);
                    insertEvCmd.Parameters.AddWithValue("@EvidenceReference", evidenceRef);
                    insertEvCmd.Parameters.AddWithValue("@EvidenceTypeCode", ev.EvidenceTypeCode);
                    insertEvCmd.Parameters.AddWithValue("@SourceTypeCode", ev.SourceTypeCode);
                    insertEvCmd.Parameters.AddWithValue("@SourceReference", (object?)ev.SourceReference ?? DBNull.Value);
                    insertEvCmd.Parameters.AddWithValue("@RuleVersion", (object?)ev.RuleVersion ?? "dpdp-v1.0.0");
                    insertEvCmd.Parameters.AddWithValue("@AgentVersion", (object?)ev.AgentVersion ?? "1.0.0");
                    insertEvCmd.Parameters.AddWithValue("@EvidenceHash", ev.EvidenceHash);
                    insertEvCmd.Parameters.AddWithValue("@HashAlgorithmCode", ev.HashAlgorithmCode);
                    insertEvCmd.Parameters.AddWithValue("@UserAccountId", command.UserAccountId);
                    insertEvCmd.Parameters.AddWithValue("@MetadataJson", (object?)ev.MetadataJson ?? DBNull.Value);

                    await insertEvCmd.ExecuteNonQueryAsync(cancellationToken);
                }
            }
            else
            {
                // Fallback deterministic evidence generation: 1 run-level + field classifications
                var runCodeSnippet = command.AnalysisRunId.ToString()[..8].ToUpperInvariant();

                // 1. Run evidence
                var runEvidenceId = Guid.NewGuid();
                var runHash = ComputeSafeSha256(
                    $"RUN|{command.AnalysisRunId}|{command.SourceObjectName}|{command.TotalRecordsAnalysed}|{command.TotalFieldsDiscovered}|{command.TotalPersonalDataFields}|{command.ClassificationCoveragePercentage}|1.0.0|dpdp-v1.0.0");

                var runMeta = JsonSerializer.Serialize(new
                {
                    sourceObjectName = command.SourceObjectName,
                    totalRecordsAnalysed = command.TotalRecordsAnalysed,
                    totalFieldsDiscovered = command.TotalFieldsDiscovered,
                    totalPersonalDataFields = command.TotalPersonalDataFields,
                    classificationCoveragePercentage = command.ClassificationCoveragePercentage,
                    processingLocation = "LOCAL_EDGE_AGENT",
                });

                await using var insertRunEvCmd =
                    new SqlCommand(insertEvidenceSql, connection, transaction);

                insertRunEvCmd.Parameters.AddWithValue("@EvidenceRecordId", runEvidenceId);
                insertRunEvCmd.Parameters.AddWithValue("@OrganisationId", command.OrganisationId);
                insertRunEvCmd.Parameters.AddWithValue("@AnalysisRunId", command.AnalysisRunId);
                insertRunEvCmd.Parameters.AddWithValue("@DiscoveredFieldId", DBNull.Value);
                insertRunEvCmd.Parameters.AddWithValue("@EvidenceReference", $"EVD-{runCodeSnippet}-RUN");
                insertRunEvCmd.Parameters.AddWithValue("@EvidenceTypeCode", "ANALYSIS_RUN");
                insertRunEvCmd.Parameters.AddWithValue("@SourceTypeCode", "EDGE_AGENT");
                insertRunEvCmd.Parameters.AddWithValue("@SourceReference", (object?)command.SourceObjectName ?? DBNull.Value);
                insertRunEvCmd.Parameters.AddWithValue("@RuleVersion", "dpdp-v1.0.0");
                insertRunEvCmd.Parameters.AddWithValue("@AgentVersion", "1.0.0");
                insertRunEvCmd.Parameters.AddWithValue("@EvidenceHash", runHash);
                insertRunEvCmd.Parameters.AddWithValue("@HashAlgorithmCode", "SHA-256");
                insertRunEvCmd.Parameters.AddWithValue("@UserAccountId", command.UserAccountId);
                insertRunEvCmd.Parameters.AddWithValue("@MetadataJson", runMeta);

                await insertRunEvCmd.ExecuteNonQueryAsync(cancellationToken);

                // 2. Field classifications
                if (command.DiscoveredFields is { Count: > 0 })
                {
                    foreach (var field in command.DiscoveredFields)
                    {
                        var fieldEvidenceId = Guid.NewGuid();
                        var fHash = ComputeSafeSha256(
                            $"FIELD|{command.AnalysisRunId}|{field.FieldName}|{field.InferredDataType}|{field.ClassificationCode ?? "UNCLASSIFIED"}|{field.ClassificationMethod}|{field.PrivacyCategory}|{field.IsRegulatedIdentifier}|1.0.0|dpdp-v1.0.0");

                        var fieldMeta = JsonSerializer.Serialize(new
                        {
                            fieldName = field.FieldName,
                            ordinalPosition = field.OrdinalPosition,
                            inferredDataType = field.InferredDataType,
                            classificationCode = field.ClassificationCode,
                            classificationMethod = field.ClassificationMethod,
                            privacyCategory = field.PrivacyCategory,
                            isPersonalData = field.IsPersonalData,
                            isRegulatedIdentifier = field.IsRegulatedIdentifier,
                            nonEmptyCount = field.NonEmptyCount,
                            emptyCount = field.EmptyCount,
                        });

                        Guid? dFieldId = fieldIdMap.TryGetValue(field.FieldName, out var mFId) ? mFId : null;
                        fieldEvidenceMap[field.FieldName] = fieldEvidenceId;

                        await using var insertFieldEvCmd =
                            new SqlCommand(insertEvidenceSql, connection, transaction);

                        insertFieldEvCmd.Parameters.AddWithValue("@EvidenceRecordId", fieldEvidenceId);
                        insertFieldEvCmd.Parameters.AddWithValue("@OrganisationId", command.OrganisationId);
                        insertFieldEvCmd.Parameters.AddWithValue("@AnalysisRunId", command.AnalysisRunId);
                        insertFieldEvCmd.Parameters.AddWithValue("@DiscoveredFieldId", (object?)dFieldId ?? DBNull.Value);
                        insertFieldEvCmd.Parameters.AddWithValue("@EvidenceReference", $"EVD-{runCodeSnippet}-COL-{field.FieldName.Replace(' ', '_')}");
                        insertFieldEvCmd.Parameters.AddWithValue("@EvidenceTypeCode", "FIELD_CLASSIFICATION");
                        insertFieldEvCmd.Parameters.AddWithValue("@SourceTypeCode", "EDGE_AGENT");
                        insertFieldEvCmd.Parameters.AddWithValue("@SourceReference", (object?)command.SourceObjectName ?? DBNull.Value);
                        insertFieldEvCmd.Parameters.AddWithValue("@RuleVersion", "dpdp-v1.0.0");
                        insertFieldEvCmd.Parameters.AddWithValue("@AgentVersion", "1.0.0");
                        insertFieldEvCmd.Parameters.AddWithValue("@EvidenceHash", fHash);
                        insertFieldEvCmd.Parameters.AddWithValue("@HashAlgorithmCode", "SHA-256");
                        insertFieldEvCmd.Parameters.AddWithValue("@UserAccountId", command.UserAccountId);
                        insertFieldEvCmd.Parameters.AddWithValue("@MetadataJson", fieldMeta);

                        await insertFieldEvCmd.ExecuteNonQueryAsync(cancellationToken);
                    }
                }
            }

            if (command.Findings is { Count: > 0 })
            {
                const string deleteExistingFindingsSql = """
                    DELETE FROM workflow.Finding
                    WHERE OrganisationId = @OrganisationId
                      AND AnalysisRunId = @AnalysisRunId;
                    """;

                await using var deleteFindingsCommand =
                    new SqlCommand(deleteExistingFindingsSql, connection, transaction);

                deleteFindingsCommand.Parameters.AddWithValue("@OrganisationId", command.OrganisationId);
                deleteFindingsCommand.Parameters.AddWithValue("@AnalysisRunId", command.AnalysisRunId);
                await deleteFindingsCommand.ExecuteNonQueryAsync(cancellationToken);

                const string insertFindingSql = """
                    INSERT INTO workflow.Finding
                    (
                        FindingId,
                        OrganisationId,
                        AnalysisRunId,
                        DiscoveredFieldId,
                        EvidenceRecordId,
                        FindingCode,
                        FindingCategory,
                        Severity,
                        FieldName,
                        RuleReference,
                        FindingStatus,
                        DetectedDateTime,
                        Message,
                        SafeMetadataJson,
                        CreatedDateTime,
                        CreatedBy
                    )
                    VALUES
                    (
                        NEWID(),
                        @OrganisationId,
                        @AnalysisRunId,
                        @DiscoveredFieldId,
                        @EvidenceRecordId,
                        @FindingCode,
                        @FindingCategory,
                        @Severity,
                        @FieldName,
                        @RuleReference,
                        N'OPEN',
                        SYSUTCDATETIME(),
                        @Message,
                        @SafeMetadataJson,
                        SYSUTCDATETIME(),
                        CONVERT(NVARCHAR(100), @UserAccountId)
                    );
                    """;

                foreach (var finding in command.Findings)
                {
                    Guid? discoveredFieldId = null;
                    if (!string.IsNullOrEmpty(finding.FieldName) && fieldIdMap.TryGetValue(finding.FieldName, out var matchedFieldId))
                    {
                        discoveredFieldId = matchedFieldId;
                    }

                    Guid? evidenceRecordId = null;
                    if (!string.IsNullOrEmpty(finding.FieldName) && fieldEvidenceMap.TryGetValue(finding.FieldName, out var matchedEvId))
                    {
                        evidenceRecordId = matchedEvId;
                    }

                    await using var insertFindingCmd =
                        new SqlCommand(insertFindingSql, connection, transaction);

                    insertFindingCmd.Parameters.AddWithValue("@OrganisationId", command.OrganisationId);
                    insertFindingCmd.Parameters.AddWithValue("@AnalysisRunId", command.AnalysisRunId);
                    insertFindingCmd.Parameters.AddWithValue("@DiscoveredFieldId", (object?)discoveredFieldId ?? DBNull.Value);
                    insertFindingCmd.Parameters.AddWithValue("@EvidenceRecordId", (object?)evidenceRecordId ?? DBNull.Value);
                    insertFindingCmd.Parameters.AddWithValue("@FindingCode", finding.FindingCode);
                    insertFindingCmd.Parameters.AddWithValue("@FindingCategory", finding.FindingCategory);
                    insertFindingCmd.Parameters.AddWithValue("@Severity", finding.Severity);
                    insertFindingCmd.Parameters.AddWithValue("@FieldName", (object?)finding.FieldName ?? DBNull.Value);
                    insertFindingCmd.Parameters.AddWithValue("@RuleReference", (object?)finding.RuleReference ?? DBNull.Value);
                    insertFindingCmd.Parameters.AddWithValue("@Message", (object?)finding.Message ?? DBNull.Value);
                    insertFindingCmd.Parameters.AddWithValue("@SafeMetadataJson", (object?)finding.SafeMetadataJson ?? DBNull.Value);
                    insertFindingCmd.Parameters.AddWithValue("@UserAccountId", command.UserAccountId);

                    await insertFindingCmd.ExecuteNonQueryAsync(cancellationToken);
                }

                // Update FindingCount on workflow.DiscoveredField
                const string updateFindingCountSql = """
                    UPDATE df
                    SET FindingCount = ISNULL(f.Cnt, 0)
                    FROM workflow.DiscoveredField df
                    LEFT JOIN (
                        SELECT FieldName, COUNT(*) AS Cnt
                        FROM workflow.Finding
                        WHERE OrganisationId = @OrganisationId AND AnalysisRunId = @AnalysisRunId
                        GROUP BY FieldName
                    ) f ON df.FieldName = f.FieldName
                    WHERE df.OrganisationId = @OrganisationId AND df.AnalysisRunId = @AnalysisRunId;
                    """;

                await using var updateFindingCountCmd =
                    new SqlCommand(updateFindingCountSql, connection, transaction);
                updateFindingCountCmd.Parameters.AddWithValue("@OrganisationId", command.OrganisationId);
                updateFindingCountCmd.Parameters.AddWithValue("@AnalysisRunId", command.AnalysisRunId);
                await updateFindingCountCmd.ExecuteNonQueryAsync(cancellationToken);
            }

                        // --------------------------------------------------------
            // Phase 5 & 6: DPDP Rules Engine Evaluation & Persistence
            // --------------------------------------------------------
            const string deleteExistingRuleEvaluationsSql = """
                DELETE FROM workflow.RuleEvaluation
                WHERE OrganisationId = @OrganisationId
                  AND AnalysisRunId = @AnalysisRunId;
                """;

            await using var deleteEvaluationsCommand =
                new SqlCommand(deleteExistingRuleEvaluationsSql, connection, transaction);

            deleteEvaluationsCommand.Parameters.AddWithValue("@OrganisationId", command.OrganisationId);
            deleteEvaluationsCommand.Parameters.AddWithValue("@AnalysisRunId", command.AnalysisRunId);
            await deleteEvaluationsCommand.ExecuteNonQueryAsync(cancellationToken);

            var ruleEvaluations = _dpdpRulesEngine.Evaluate(command, command.DiscoveredFields);

            const string insertRuleEvaluationSql = """
                INSERT INTO workflow.RuleEvaluation
                (
                    RuleEvaluationId,
                    OrganisationId,
                    AnalysisRunId,
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
                )
                VALUES
                (
                    @RuleEvaluationId,
                    @OrganisationId,
                    @AnalysisRunId,
                    @FrameworkCode,
                    @FrameworkVersion,
                    @RuleCode,
                    @RuleVersion,
                    @RuleName,
                    @RegulatoryReference,
                    @EvaluationOutcome,
                    @Severity,
                    @SummaryMessage,
                    @EvaluatedFieldsCount,
                    @FlaggedFieldsCount,
                    @FlaggedFieldNamesJson,
                    SYSUTCDATETIME(),
                    SYSUTCDATETIME(),
                    CONVERT(NVARCHAR(100), @UserAccountId)
                );
                """;

            foreach (var eval in ruleEvaluations)
            {
                var evalId = Guid.NewGuid();
                await using var insertEvalCommand =
                    new SqlCommand(insertRuleEvaluationSql, connection, transaction);

                insertEvalCommand.Parameters.AddWithValue("@RuleEvaluationId", evalId);
                insertEvalCommand.Parameters.AddWithValue("@OrganisationId", command.OrganisationId);
                insertEvalCommand.Parameters.AddWithValue("@AnalysisRunId", command.AnalysisRunId);
                insertEvalCommand.Parameters.AddWithValue("@FrameworkCode", eval.FrameworkCode);
                insertEvalCommand.Parameters.AddWithValue("@FrameworkVersion", eval.FrameworkVersion);
                insertEvalCommand.Parameters.AddWithValue("@RuleCode", eval.RuleCode);
                insertEvalCommand.Parameters.AddWithValue("@RuleVersion", eval.RuleVersion);
                insertEvalCommand.Parameters.AddWithValue("@RuleName", eval.RuleName);
                insertEvalCommand.Parameters.AddWithValue("@RegulatoryReference", (object?)eval.RegulatoryReference ?? DBNull.Value);
                insertEvalCommand.Parameters.AddWithValue("@EvaluationOutcome", eval.EvaluationOutcome);
                insertEvalCommand.Parameters.AddWithValue("@Severity", eval.Severity);
                insertEvalCommand.Parameters.AddWithValue("@SummaryMessage", eval.SummaryMessage);
                insertEvalCommand.Parameters.AddWithValue("@EvaluatedFieldsCount", eval.EvaluatedFieldsCount);
                insertEvalCommand.Parameters.AddWithValue("@FlaggedFieldsCount", eval.FlaggedFieldsCount);
                insertEvalCommand.Parameters.AddWithValue("@FlaggedFieldNamesJson", (object?)System.Text.Json.JsonSerializer.Serialize(eval.FlaggedFieldNames) ?? DBNull.Value);
                insertEvalCommand.Parameters.AddWithValue("@UserAccountId", command.UserAccountId);

                await insertEvalCommand.ExecuteNonQueryAsync(cancellationToken);

                // Auto-create finding if review required or failed and finding not already reported
                if (!string.Equals(eval.EvaluationOutcome, "PASS", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(eval.EvaluationOutcome, "NOT_APPLICABLE", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrEmpty(eval.RecommendedFindingCode))
                {
                    var alreadyReported = command.Findings != null &&
                        command.Findings.Any(f => string.Equals(f.RuleReference, eval.RuleCode, StringComparison.OrdinalIgnoreCase));

                    if (!alreadyReported)
                    {
                        const string insertAutoFindingSql = """
                            INSERT INTO workflow.Finding
                            (
                                FindingId,
                                OrganisationId,
                                AnalysisRunId,
                                DiscoveredFieldId,
                                FindingCode,
                                FindingCategory,
                                Severity,
                                FieldName,
                                RuleReference,
                                FindingStatus,
                                DetectedDateTime,
                                Message,
                                SafeMetadataJson,
                                CreatedDateTime,
                                CreatedBy
                            )
                            VALUES
                            (
                                @FindingId,
                                @OrganisationId,
                                @AnalysisRunId,
                                NULL,
                                @FindingCode,
                                @FindingCategory,
                                @Severity,
                                @FieldName,
                                @RuleReference,
                                N'OPEN',
                                SYSUTCDATETIME(),
                                @Message,
                                @SafeMetadataJson,
                                SYSUTCDATETIME(),
                                CONVERT(NVARCHAR(100), @UserAccountId)
                            );
                            """;

                        var findingSeverity = string.Equals(eval.Severity, "HIGH", StringComparison.OrdinalIgnoreCase)
                            ? "WARNING"
                            : "INFO";

                        await using var autoFindingCmd =
                            new SqlCommand(insertAutoFindingSql, connection, transaction);

                        autoFindingCmd.Parameters.AddWithValue("@FindingId", Guid.NewGuid());
                        autoFindingCmd.Parameters.AddWithValue("@OrganisationId", command.OrganisationId);
                        autoFindingCmd.Parameters.AddWithValue("@AnalysisRunId", command.AnalysisRunId);
                        autoFindingCmd.Parameters.AddWithValue("@FindingCode", eval.RecommendedFindingCode);
                        autoFindingCmd.Parameters.AddWithValue("@FindingCategory", eval.RecommendedFindingCategory ?? "REGULATORY");
                        autoFindingCmd.Parameters.AddWithValue("@Severity", findingSeverity);
                        autoFindingCmd.Parameters.AddWithValue("@FieldName", eval.FlaggedFieldNames.Count > 0 ? (object?)string.Join(", ", eval.FlaggedFieldNames) : DBNull.Value);
                        autoFindingCmd.Parameters.AddWithValue("@RuleReference", eval.RuleCode);
                        autoFindingCmd.Parameters.AddWithValue("@Message", eval.SummaryMessage);
                        autoFindingCmd.Parameters.AddWithValue("@SafeMetadataJson", (object?)System.Text.Json.JsonSerializer.Serialize(new { ruleVersion = eval.RuleVersion, outcome = eval.EvaluationOutcome }) ?? DBNull.Value);
                        autoFindingCmd.Parameters.AddWithValue("@UserAccountId", command.UserAccountId);

                        await autoFindingCmd.ExecuteNonQueryAsync(cancellationToken);
                    }
                }

                // Generate safe SHA-256 evidence record for this rule evaluation
                var runCodeSnippet = command.AnalysisRunId.ToString()[..8].ToUpperInvariant();
                var evalEvidenceId = Guid.NewGuid();
                var evalHash = ComputeSafeSha256($"EVAL|{eval.RuleCode}|{eval.RuleVersion}|{eval.EvaluationOutcome}|{eval.SummaryMessage}");

                const string insertEvalEvidenceSql = """
                    INSERT INTO evidence.EvidenceRecord
                    (
                        EvidenceRecordId,
                        OrganisationId,
                        AnalysisRunId,
                        DiscoveredFieldId,
                        EvidenceReference,
                        EvidenceTypeCode,
                        SourceTypeCode,
                        SourceReference,
                        RuleVersion,
                        AgentVersion,
                        EvidenceHash,
                        HashAlgorithmCode,
                        StorageReference,
                        CapturedDateTime,
                        CapturedBy,
                        IsVerified,
                        VerifiedDateTime,
                        VerifiedBy,
                        VerificationStatusCode,
                        MetadataJson,
                        CreatedDateTime
                    )
                    VALUES
                    (
                        @EvidenceRecordId,
                        @OrganisationId,
                        @AnalysisRunId,
                        NULL,
                        @EvidenceReference,
                        N'RULE_EVALUATION',
                        N'CONTROL_PLANE',
                        @SourceReference,
                        @RuleVersion,
                        N'1.0.0',
                        @EvidenceHash,
                        N'SHA-256',
                        NULL,
                        SYSUTCDATETIME(),
                        CONVERT(NVARCHAR(150), @UserAccountId),
                        1,
                        SYSUTCDATETIME(),
                        N'PrivacyComply.ControlPlane',
                        N'VERIFIED',
                        @MetadataJson,
                        SYSUTCDATETIME()
                    );
                    """;

                await using var evalEvCmd =
                    new SqlCommand(insertEvalEvidenceSql, connection, transaction);

                evalEvCmd.Parameters.AddWithValue("@EvidenceRecordId", evalEvidenceId);
                evalEvCmd.Parameters.AddWithValue("@OrganisationId", command.OrganisationId);
                evalEvCmd.Parameters.AddWithValue("@AnalysisRunId", command.AnalysisRunId);
                evalEvCmd.Parameters.AddWithValue("@EvidenceReference", $"EVD-{runCodeSnippet}-{eval.RuleCode}-EVAL");
                evalEvCmd.Parameters.AddWithValue("@SourceReference", (object?)eval.RegulatoryReference ?? DBNull.Value);
                evalEvCmd.Parameters.AddWithValue("@RuleVersion", eval.RuleVersion);
                evalEvCmd.Parameters.AddWithValue("@EvidenceHash", evalHash);
                evalEvCmd.Parameters.AddWithValue("@UserAccountId", command.UserAccountId);
                evalEvCmd.Parameters.AddWithValue("@MetadataJson", (object?)System.Text.Json.JsonSerializer.Serialize(new
                {
                    ruleCode = eval.RuleCode,
                    ruleVersion = eval.RuleVersion,
                    outcome = eval.EvaluationOutcome,
                    severity = eval.Severity
                }) ?? DBNull.Value);

                await evalEvCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task FailAsync(
        FailAnalysisRunCommand command,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            await _sqlConnectionFactory.OpenConnectionAsync(
                cancellationToken);

        const string sql = """
            UPDATE workflow.AnalysisRun
            SET
                RunStatusCode = N'FAILED',
                FailedDateTime = SYSUTCDATETIME(),
                FailureCode = @FailureCode,
                FailureMessage = NULL,
                UpdatedDateTime = SYSUTCDATETIME(),
                UpdatedBy = CONVERT(
                    NVARCHAR(100),
                    @UserAccountId
                )
            WHERE
                OrganisationId = @OrganisationId
                AND AnalysisRunId = @AnalysisRunId
                AND RunStatusCode IN (
                    N'QUEUED',
                    N'RUNNING'
                )
                AND IsDeleted = 0;
            """;

        await using var commandSql =
            new SqlCommand(sql, connection);

        commandSql.Parameters.AddWithValue(
            "@OrganisationId",
            command.OrganisationId);

        commandSql.Parameters.AddWithValue(
            "@AnalysisRunId",
            command.AnalysisRunId);

        commandSql.Parameters.AddWithValue(
            "@UserAccountId",
            command.UserAccountId);

        commandSql.Parameters.AddWithValue(
            "@FailureCode",
            command.FailureCode);

        var affectedRows =
            await commandSql.ExecuteNonQueryAsync(
                cancellationToken);

        if (affectedRows != 1)
        {
            throw new InvalidOperationException(
                "The analysis run could not be failed. It may not exist or may already be in a terminal state.");
        }
    }

    private static string ComputeSafeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
