namespace PrivacyComply.Api.Endpoints.Runs;

public sealed record CreateAnalysisRunRequest(
    string SourceTypeCode,
    string SourceName,
    string? SourceObjectName,
    long? TotalRecordsAnalysed,
    int? TotalFieldsDiscovered,
    int? TotalUnclassifiedFields,
    decimal? ClassificationCoveragePercentage);