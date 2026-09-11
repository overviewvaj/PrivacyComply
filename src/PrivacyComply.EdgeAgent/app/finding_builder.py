from typing import Any


def build_findings(
    column_profiles: list[dict[str, Any]],
) -> list[dict[str, Any]]:
    findings: list[dict[str, Any]] = []

    for profile in column_profiles:
        empty_count = profile["emptyCount"]

        if empty_count > 0:
            findings.append(
                {
                    "findingCode":
                        "MISSING_VALUES_DETECTED",
                    "severity":
                        "INFO",
                    "columnName":
                        profile["columnName"],
                    "classificationCode":
                        profile["classificationCode"],
                    "emptyCount":
                        empty_count,
                    "message":
                        "One or more empty values "
                        "were detected in this column.",
                }
            )

        if (
            profile["classificationStatus"]
            == "UNCLASSIFIED"
        ):
            findings.append(
                {
                    "findingCode":
                        "UNCLASSIFIED_COLUMN",
                    "severity":
                        "WARNING",
                    "columnName":
                        profile["columnName"],
                    "classificationCode":
                        None,
                    "emptyCount":
                        profile["emptyCount"],
                    "message":
                        "This column could not be "
                        "classified automatically.",
                }
            )

        if (
            profile["classificationStatus"]
            == "CLASSIFIED"
            and profile["classificationMethod"]
            == "VALUE_PATTERN_RULE"
        ):
            findings.append(
                {
                    "findingCode":
                        "VALUE_PATTERN_CLASSIFICATION",
                    "severity":
                        "INFO",
                    "columnName":
                        profile["columnName"],
                    "classificationCode":
                        profile["classificationCode"],
                    "emptyCount":
                        profile["emptyCount"],
                    "message":
                        "This column was classified "
                        "using local value-pattern analysis.",
                }
            )

    return findings


def build_finding_summary(
    findings: list[dict[str, Any]],
) -> dict[str, Any]:
    severity_counts = {
        "INFO": 0,
        "WARNING": 0,
        "ERROR": 0,
    }

    for finding in findings:
        severity = finding.get(
            "severity"
        )

        if severity in severity_counts:
            severity_counts[severity] += 1

    return {
        "totalFindings": len(findings),
        "infoCount":
            severity_counts["INFO"],
        "warningCount":
            severity_counts["WARNING"],
        "errorCount":
            severity_counts["ERROR"],
    }