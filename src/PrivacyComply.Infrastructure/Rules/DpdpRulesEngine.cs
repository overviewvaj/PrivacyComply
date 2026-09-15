namespace PrivacyComply.Infrastructure.Rules;

using PrivacyComply.Application.Features.Rules;
using PrivacyComply.Application.Features.Runs.Commands;

public sealed class DpdpRulesEngine : IDpdpRulesEngine
{
    private const string FrameworkCode = "DPDP";
    private const string FrameworkVersion = "2023-v1.0";
    private const string RuleVersion = "1.0.0";

    public IReadOnlyList<DpdpRuleEvaluationResult> Evaluate(
        CompleteAnalysisRunCommand command,
        IReadOnlyList<DiscoveredFieldCommand>? fields)
    {
        var evaluatedFields = fields ?? Array.Empty<DiscoveredFieldCommand>();
        var results = new List<DpdpRuleEvaluationResult>(5);

        // ------------------------------------------------------------
        // Rule 1: DPDP-SEC-8-01 (Reasonable Security Safeguards - Direct Identifiers)
        // ------------------------------------------------------------
        var regulatedDirectIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "GOVT_ID_AADHAAR",
            "GOVT_ID_PAN",
            "GOVT_ID_PASSPORT",
            "GOVT_ID_VOTER_ID",
            "FINANCIAL_CREDIT_CARD"
        };

        var directIdFields = evaluatedFields
            .Where(f => f.IsRegulatedIdentifier ||
                        (!string.IsNullOrEmpty(f.ClassificationCode) && regulatedDirectIds.Contains(f.ClassificationCode)))
            .Select(f => f.FieldName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (directIdFields.Count > 0)
        {
            results.Add(new DpdpRuleEvaluationResult(
                FrameworkCode,
                FrameworkVersion,
                "DPDP-SEC-8-01",
                RuleVersion,
                "Reasonable Security Safeguards — Regulated Identifiers",
                "DPDP Act 2023, Section 8(5)",
                "REVIEW_REQUIRED",
                "HIGH",
                $"Direct statutory identifier(s) detected ({string.Join(", ", directIdFields)}). Section 8(5) mandates reasonable security safeguards, access restrictions, and at-rest cryptographic protection for government and direct personal identifiers.",
                evaluatedFields.Count,
                directIdFields.Count,
                directIdFields,
                "REGULATED_IDENTIFIER_SAFEGUARD_REVIEW",
                "REGULATORY"));
        }
        else
        {
            results.Add(new DpdpRuleEvaluationResult(
                FrameworkCode,
                FrameworkVersion,
                "DPDP-SEC-8-01",
                RuleVersion,
                "Reasonable Security Safeguards — Regulated Identifiers",
                "DPDP Act 2023, Section 8(5)",
                "PASS",
                "INFO",
                "No unmasked direct government or statutory identifiers detected in the catalogued dataset schema.",
                evaluatedFields.Count,
                0,
                Array.Empty<string>()));
        }

        // ------------------------------------------------------------
        // Rule 2: DPDP-SEC-4-01 (Personal Data Cataloguing & Purpose Limitation)
        // ------------------------------------------------------------
        var unclassifiedFields = evaluatedFields
            .Where(f => !string.Equals(f.ClassificationStatus, "CLASSIFIED", StringComparison.OrdinalIgnoreCase) ||
                        string.IsNullOrWhiteSpace(f.ClassificationCode))
            .Select(f => f.FieldName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (unclassifiedFields.Count > 0)
        {
            results.Add(new DpdpRuleEvaluationResult(
                FrameworkCode,
                FrameworkVersion,
                "DPDP-SEC-4-01",
                RuleVersion,
                "Personal Data Cataloguing & Purpose Limitation",
                "DPDP Act 2023, Section 4",
                "REVIEW_REQUIRED",
                "MEDIUM",
                $"Data catalogue contains {unclassifiedFields.Count} unclassified field(s) ({string.Join(", ", unclassifiedFields)}). Section 4 mandates lawful purpose limitation, requiring classification of all processed personal attributes.",
                evaluatedFields.Count,
                unclassifiedFields.Count,
                unclassifiedFields,
                "UNCLASSIFIED_FIELDS_PURPOSE_REVIEW",
                "CLASSIFICATION"));
        }
        else
        {
            results.Add(new DpdpRuleEvaluationResult(
                FrameworkCode,
                FrameworkVersion,
                "DPDP-SEC-4-01",
                RuleVersion,
                "Personal Data Cataloguing & Purpose Limitation",
                "DPDP Act 2023, Section 4",
                "PASS",
                "INFO",
                "All discovered schema fields have been deterministically categorized and mapped to verified privacy classifications.",
                evaluatedFields.Count,
                0,
                Array.Empty<string>()));
        }

        // ------------------------------------------------------------
        // Rule 3: DPDP-SEC-5-01 (Notice & Consent Prerequisite Verification)
        // ------------------------------------------------------------
        var personalDataFields = evaluatedFields
            .Where(f => f.IsPersonalData || string.Equals(f.PrivacyCategory, "PERSONAL_DATA", StringComparison.OrdinalIgnoreCase))
            .Select(f => f.FieldName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (personalDataFields.Count == 0)
        {
            results.Add(new DpdpRuleEvaluationResult(
                FrameworkCode,
                FrameworkVersion,
                "DPDP-SEC-5-01",
                RuleVersion,
                "Notice & Verifiable Consent Tracking Prerequisite",
                "DPDP Act 2023, Section 5 & Section 6",
                "NOT_APPLICABLE",
                "INFO",
                "No personal data columns detected in dataset; statutory notice and consent prerequisites under Section 5 & 6 are not applicable.",
                evaluatedFields.Count,
                0,
                Array.Empty<string>()));
        }
        else
        {
            var consentField = evaluatedFields.FirstOrDefault(f =>
                f.FieldName.Contains("Consent", StringComparison.OrdinalIgnoreCase) ||
                f.FieldName.Contains("Notice", StringComparison.OrdinalIgnoreCase) ||
                f.FieldName.Contains("OptIn", StringComparison.OrdinalIgnoreCase) ||
                f.FieldName.Contains("Opt_In", StringComparison.OrdinalIgnoreCase));

            if (consentField != null)
            {
                results.Add(new DpdpRuleEvaluationResult(
                    FrameworkCode,
                    FrameworkVersion,
                    "DPDP-SEC-5-01",
                    RuleVersion,
                    "Notice & Verifiable Consent Tracking Prerequisite",
                    "DPDP Act 2023, Section 5 & Section 6",
                    "PASS",
                    "INFO",
                    $"Verifiable consent tracking attribute ({consentField.FieldName}) detected alongside personal data records.",
                    evaluatedFields.Count,
                    0,
                    Array.Empty<string>()));
            }
            else
            {
                results.Add(new DpdpRuleEvaluationResult(
                    FrameworkCode,
                    FrameworkVersion,
                    "DPDP-SEC-5-01",
                    RuleVersion,
                    "Notice & Verifiable Consent Tracking Prerequisite",
                    "DPDP Act 2023, Section 5 & Section 6",
                    "REVIEW_REQUIRED",
                    "MEDIUM",
                    $"Personal data attributes identified across {personalDataFields.Count} columns without schema-level consent tracking or notice metadata. Audit notice itemisation and verifiable consent capture pursuant to Section 5 & 6.",
                    evaluatedFields.Count,
                    personalDataFields.Count,
                    personalDataFields,
                    "CONSENT_TRACKING_METADATA_ABSENT",
                    "PRIVACY"));
            }
        }

        // ------------------------------------------------------------
        // Rule 4: DPDP-SEC-8-07 (Personal Data Quality & Minimisation Mandate)
        // ------------------------------------------------------------
        var incompletePdFields = evaluatedFields
            .Where(f => (f.IsPersonalData || string.Equals(f.PrivacyCategory, "PERSONAL_DATA", StringComparison.OrdinalIgnoreCase)) &&
                        f.EmptyCount > 0)
            .Select(f => f.FieldName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (incompletePdFields.Count > 0)
        {
            results.Add(new DpdpRuleEvaluationResult(
                FrameworkCode,
                FrameworkVersion,
                "DPDP-SEC-8-07",
                RuleVersion,
                "Personal Data Quality & Minimisation Mandate",
                "DPDP Act 2023, Section 8(3)",
                "REVIEW_REQUIRED",
                "LOW",
                $"Missing/empty values detected in {incompletePdFields.Count} personal data column(s) ({string.Join(", ", incompletePdFields)}). Section 8(3) mandates that processed personal data must be accurate, complete, and consistent.",
                evaluatedFields.Count,
                incompletePdFields.Count,
                incompletePdFields,
                "PERSONAL_DATA_INCOMPLETE_VALUES",
                "DATA_QUALITY"));
        }
        else
        {
            results.Add(new DpdpRuleEvaluationResult(
                FrameworkCode,
                FrameworkVersion,
                "DPDP-SEC-8-07",
                RuleVersion,
                "Personal Data Quality & Minimisation Mandate",
                "DPDP Act 2023, Section 8(3)",
                "PASS",
                "INFO",
                "Personal data columns exhibit 100% data completeness with zero detected null or empty value anomalies.",
                evaluatedFields.Count,
                0,
                Array.Empty<string>()));
        }

        // ------------------------------------------------------------
        // Rule 5: DPDP-SEC-8-09 (Storage Limitation & Retention Linkage)
        // ------------------------------------------------------------
        if (personalDataFields.Count > 0)
        {
            results.Add(new DpdpRuleEvaluationResult(
                FrameworkCode,
                FrameworkVersion,
                "DPDP-SEC-8-09",
                RuleVersion,
                "Storage Limitation & Retention Schedule Linkage",
                "DPDP Act 2023, Section 8(7)",
                "REVIEW_REQUIRED",
                "LOW",
                $"Personal data detected across {personalDataFields.Count} column(s). Pursuant to Section 8(7), data fiduciaries must link active retention and erasure policies to avoid unlawful retention after purpose completion.",
                evaluatedFields.Count,
                personalDataFields.Count,
                personalDataFields,
                "RETENTION_POLICY_LINKAGE_REQUIRED",
                "REGULATORY"));
        }
        else
        {
            results.Add(new DpdpRuleEvaluationResult(
                FrameworkCode,
                FrameworkVersion,
                "DPDP-SEC-8-09",
                RuleVersion,
                "Storage Limitation & Retention Schedule Linkage",
                "DPDP Act 2023, Section 8(7)",
                "NOT_APPLICABLE",
                "INFO",
                "No personal data identified; statutory storage limitation under Section 8(7) is not applicable.",
                evaluatedFields.Count,
                0,
                Array.Empty<string>()));
        }

        return results;
    }
}
