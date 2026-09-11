namespace PrivacyComply.Application.Features.Runs.Queries;

public interface IAnalysisRunQueries
{
    Task<IReadOnlyList<AnalysisRunSummary>>
        GetAllAsync(
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