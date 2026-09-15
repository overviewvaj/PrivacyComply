namespace PrivacyComply.Application.Features.Rules;

using PrivacyComply.Application.Features.Runs.Commands;

public sealed record DpdpRuleEvaluationResult(
    string FrameworkCode,
    string FrameworkVersion,
    string RuleCode,
    string RuleVersion,
    string RuleName,
    string? RegulatoryReference,
    string EvaluationOutcome, // 'PASS', 'FAIL', 'REVIEW_REQUIRED', 'NOT_APPLICABLE'
    string Severity,          // 'HIGH', 'MEDIUM', 'LOW', 'INFO'
    string SummaryMessage,
    int EvaluatedFieldsCount,
    int FlaggedFieldsCount,
    IReadOnlyList<string> FlaggedFieldNames,
    string? RecommendedFindingCode = null,
    string? RecommendedFindingCategory = null);

public interface IDpdpRulesEngine
{
    IReadOnlyList<DpdpRuleEvaluationResult> Evaluate(
        CompleteAnalysisRunCommand command,
        IReadOnlyList<DiscoveredFieldCommand>? fields);
}
