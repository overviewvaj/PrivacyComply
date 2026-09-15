namespace PrivacyComply.Application.Features.Runs.Queries;

public interface IAnalysisRunQueries
{
    Task<IReadOnlyList<AnalysisRunSummary>>
        GetAllAsync(
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiscoveredFieldDto>>
        GetDiscoveredFieldsAsync(
            Guid organisationId,
            Guid analysisRunId,
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FindingDto>>
        GetFindingsAsync(
            Guid organisationId,
            Guid analysisRunId,
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EvidenceDto>>
        GetEvidenceAsync(
            Guid organisationId,
            Guid analysisRunId,
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RuleEvaluationDto>>
        GetRuleEvaluationsAsync(
            Guid organisationId,
            Guid analysisRunId,
            CancellationToken cancellationToken = default);
}

public sealed record AnalysisRunSummary(
    Guid AnalysisRunId,
    string AnalysisRunCode,
    string RunStatusCode,
    string SourceTypeCode,
    string? SourceName,
    string? SourceObjectName,
    DateTime RequestedDateTime,
    DateTime? StartedDateTime,
    DateTime? CompletedDateTime,
    DateTime? FailedDateTime,
    DateTime? CancelledDateTime,
    long? TotalRecordsAnalysed,
    int? TotalFieldsDiscovered,
    int? TotalPersonalDataFields,
    int? TotalSensitiveDataFields,
    int? TotalUnclassifiedFields,
    decimal? ClassificationCoveragePercentage);

public sealed record DiscoveredFieldDto(
    Guid DiscoveredFieldId,
    Guid AnalysisRunId,
    string? SourceObjectName,
    string FieldName,
    int OrdinalPosition,
    string InferredDataType,
    string ClassificationStatus,
    string? ClassificationCode,
    string ClassificationMethod,
    decimal? MatchPercentage,
    string PrivacyCategory,
    bool IsPersonalData,
    bool IsRegulatedIdentifier,
    long NonEmptyCount,
    long EmptyCount,
    int FindingCount,
    DateTime CreatedDateTime);

public sealed record FindingDto(
    Guid FindingId,
    Guid AnalysisRunId,
    Guid? DiscoveredFieldId,
    string FindingCode,
    string FindingCategory,
    string Severity,
    string? FieldName,
    string? RuleReference,
    string FindingStatus,
    DateTime DetectedDateTime,
    DateTime? ResolvedDateTime,
    string? Message,
    string? SafeMetadataJson,
    DateTime CreatedDateTime);

public sealed record EvidenceDto(
    Guid EvidenceId,
    Guid OrganisationId,
    Guid AnalysisRunId,
    Guid? DiscoveredFieldId,
    string EvidenceReference,
    string EvidenceType,
    string SourceType,
    string? SourceReference,
    string? RuleVersion,
    string? AgentVersion,
    string EvidenceHash,
    string HashAlgorithmCode,
    DateTime GeneratedDateTime,
    string? CapturedBy,
    bool IsVerified,
    string VerificationStatusCode,
    string? MetadataJson,
    string? FieldName,
    string? SourceObjectName,
    string? ClassificationCode,
    string? PrivacyCategory,
    bool? IsPersonalData,
    bool? IsRegulatedIdentifier);

public sealed record RuleEvaluationDto(
    Guid RuleEvaluationId,
    Guid OrganisationId,
    Guid AnalysisRunId,
    string? AnalysisRunCode,
    string FrameworkCode,
    string FrameworkVersion,
    string RuleCode,
    string RuleVersion,
    string RuleName,
    string? RegulatoryReference,
    string EvaluationOutcome,
    string Severity,
    string SummaryMessage,
    int EvaluatedFieldsCount,
    int FlaggedFieldsCount,
    string? FlaggedFieldNamesJson,
    DateTime EvaluatedDateTime,
    DateTime CreatedDateTime,
    string CreatedBy);
