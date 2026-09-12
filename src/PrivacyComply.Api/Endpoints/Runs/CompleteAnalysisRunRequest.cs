namespace PrivacyComply.Api.Endpoints.Runs;

public sealed record CompleteAnalysisRunRequest(
    string? SourceObjectName,
    long TotalRecordsAnalysed,
    int TotalFieldsDiscovered,
    int TotalUnclassifiedFields,
    decimal ClassificationCoveragePercentage);