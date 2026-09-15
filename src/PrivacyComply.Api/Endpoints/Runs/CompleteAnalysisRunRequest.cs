namespace PrivacyComply.Api.Endpoints.Runs;

public sealed record DiscoveredFieldRequest(
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
    long EmptyCount);

public sealed record FindingRequest(
    string FindingCode,
    string FindingCategory,
    string Severity,
    string? FieldName,
    string? RuleReference,
    string? Message,
    string? SafeMetadataJson);

public sealed record EvidenceRecordRequest(
    string EvidenceReference,
    string EvidenceTypeCode,
    string SourceTypeCode,
    string? SourceReference,
    string? FieldName,
    string? ClassificationCode,
    string? RuleVersion,
    string? AgentVersion,
    string EvidenceHash,
    string HashAlgorithmCode,
    string? MetadataJson);

public sealed record CompleteAnalysisRunRequest(
    string? SourceObjectName,
    long TotalRecordsAnalysed,
    int TotalFieldsDiscovered,
    int TotalPersonalDataFields,
    int TotalUnclassifiedFields,
    decimal ClassificationCoveragePercentage,
    IReadOnlyList<DiscoveredFieldRequest>? DiscoveredFields = null,
    IReadOnlyList<FindingRequest>? Findings = null,
    IReadOnlyList<EvidenceRecordRequest>? Evidence = null);
