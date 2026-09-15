namespace PrivacyComply.Application.Features.Runs.Commands;

public interface IAnalysisRunCommands
{
    Task<CreateAnalysisRunResult> CreateAsync(
        CreateAnalysisRunCommand command,
        CancellationToken cancellationToken = default);

    Task StartAsync(
        StartAnalysisRunCommand command,
        CancellationToken cancellationToken = default);

    Task CompleteAsync(
        CompleteAnalysisRunCommand command,
        CancellationToken cancellationToken = default);

    Task FailAsync(
        FailAnalysisRunCommand command,
        CancellationToken cancellationToken = default);
}

public sealed record CreateAnalysisRunCommand(
    Guid OrganisationId,
    string SourceTypeCode,
    string SourceName,
    Guid RequestedByUserAccountId,
    string? CorrelationId);

public sealed record CreateAnalysisRunResult(
    Guid AnalysisRunId,
    string AnalysisRunCode,
    string RunStatusCode,
    DateTime RequestedDateTime);

public sealed record StartAnalysisRunCommand(
    Guid OrganisationId,
    Guid AnalysisRunId,
    Guid UserAccountId);

public sealed record DiscoveredFieldCommand(
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

public sealed record FindingCommand(
    string FindingCode,
    string FindingCategory,
    string Severity,
    string? FieldName,
    string? RuleReference,
    string? Message,
    string? SafeMetadataJson);

public sealed record EvidenceRecordCommand(
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

public sealed record CompleteAnalysisRunCommand(
    Guid OrganisationId,
    Guid AnalysisRunId,
    Guid UserAccountId,
    string? SourceObjectName,
    long TotalRecordsAnalysed,
    int TotalFieldsDiscovered,
    int TotalPersonalDataFields,
    int TotalUnclassifiedFields,
    decimal ClassificationCoveragePercentage,
    IReadOnlyList<DiscoveredFieldCommand>? DiscoveredFields = null,
    IReadOnlyList<FindingCommand>? Findings = null,
    IReadOnlyList<EvidenceRecordCommand>? Evidence = null);

public sealed record FailAnalysisRunCommand(
    Guid OrganisationId,
    Guid AnalysisRunId,
    Guid UserAccountId,
    string FailureCode);
