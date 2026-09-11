namespace PrivacyComply.Application.Features.Runs.Commands;

public interface ICancelAnalysisRunCommand
{
    Task<CancelAnalysisRunResult> CancelAsync(
        CancelAnalysisRunRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record CancelAnalysisRunRequest(
    Guid OrganisationId,
    Guid AnalysisRunId,
    Guid RequestedByUserAccountId);

public sealed record CancelAnalysisRunResult(
    Guid AnalysisRunId,
    string AnalysisRunCode,
    string RunStatusCode);