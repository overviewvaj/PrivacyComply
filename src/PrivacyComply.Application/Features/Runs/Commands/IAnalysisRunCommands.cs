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

public sealed record CompleteAnalysisRunCommand(
    Guid OrganisationId,
    Guid AnalysisRunId,
    Guid UserAccountId,
    string? SourceObjectName,
    long TotalRecordsAnalysed,
    int TotalFieldsDiscovered,
    int TotalUnclassifiedFields,
    decimal ClassificationCoveragePercentage);

public sealed record FailAnalysisRunCommand(
    Guid OrganisationId,
    Guid AnalysisRunId,
    Guid UserAccountId,
    string FailureCode);