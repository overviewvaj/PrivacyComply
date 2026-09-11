namespace PrivacyComply.Application.Features.Runs.Commands;

public interface IAnalysisRunCommands
{
    Task<CreateAnalysisRunResult> CreateAsync(
        CreateAnalysisRunCommand command,
        CancellationToken cancellationToken = default);
}

public sealed record CreateAnalysisRunCommand(
    Guid OrganisationId,
    string SourceTypeCode,
    string SourceName,
    string? SourceObjectName,
    Guid RequestedByUserAccountId,
    string? CorrelationId);

public sealed record CreateAnalysisRunResult(
    Guid AnalysisRunId,
    string AnalysisRunCode,
    string RunStatusCode,
    DateTime RequestedDateTime);