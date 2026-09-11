namespace PrivacyComply.Api.Endpoints.Runs;

public sealed record CreateAnalysisRunRequest(
    string SourceTypeCode,
    string SourceName,
    string? SourceObjectName);