from collections import Counter
from typing import Any


def build_findings(
    column_profiles: list[dict[str, Any]],
) -> list[dict[str, Any]]:
    findings: list[dict[str, Any]] = []

    column_name_counts = Counter(
        profile["columnName"].strip().lower()
        for profile in column_profiles
        if profile["columnName"].strip()
    )

    duplicate_names_reported: set[str] = set()

    for profile in column_profiles:
        column_name = profile["columnName"]
        normalized_column_name = (
            column_name.strip().lower()
        )

        empty_count = profile["emptyCount"]
        non_empty_count = profile["nonEmptyCount"]

        classification_status = (
            profile["classificationStatus"]
        )

        classification_code = (
            profile["classificationCode"]
        )

        classification_method = (
            profile["classificationMethod"]
        )

        match_percentage = (
            profile.get("matchPercentage")
        )

        inferred_data_type = (
            profile["inferredDataType"]
        )

        if not column_name.strip():
            findings.append(
                {
                    "findingCode":
                        "BLANK_COLUMN_NAME",
                    "findingCategory":
                        "DATA_QUALITY",
                    "severity":
                        "WARNING",
                    "columnName":
                        column_name,
                    "classificationCode":
                        classification_code,
                    "emptyCount":
                        empty_count,
                    "message":
                        "This column has a blank "
                        "column name.",
                }
            )

        if (
            normalized_column_name
            and column_name_counts[
                normalized_column_name
            ] > 1
            and normalized_column_name
            not in duplicate_names_reported
        ):
            findings.append(
                {
                    "findingCode":
                        "DUPLICATE_COLUMN_NAME",
                    "findingCategory":
                        "DATA_QUALITY",
                    "severity":
                        "WARNING",
                    "columnName":
                        column_name,
                    "classificationCode":
                        classification_code,
                    "emptyCount":
                        empty_count,
                    "message":
                        "This column name appears "
                        "more than once in the dataset.",
                }
            )

            duplicate_names_reported.add(
                normalized_column_name
            )

        if non_empty_count == 0:
            findings.append(
                {
                    "findingCode":
                        "EMPTY_COLUMN",
                    "findingCategory":
                        "DATA_QUALITY",
                    "severity":
                        "WARNING",
                    "columnName":
                        column_name,
                    "classificationCode":
                        classification_code,
                    "emptyCount":
                        empty_count,
                    "message":
                        "This column contains no "
                        "non-empty values.",
                }
            )

        elif empty_count > 0:
            findings.append(
                {
                    "findingCode":
                        "MISSING_VALUES_DETECTED",
                    "findingCategory":
                        "DATA_QUALITY",
                    "severity":
                        "INFO",
                    "columnName":
                        column_name,
                    "classificationCode":
                        classification_code,
                    "emptyCount":
                        empty_count,
                    "message":
                        "One or more empty values "
                        "were detected in this column.",
                }
            )

        if inferred_data_type == "MIXED":
            findings.append(
                {
                    "findingCode":
                        "MIXED_DATA_TYPES",
                    "findingCategory":
                        "DATA_QUALITY",
                    "severity":
                        "WARNING",
                    "columnName":
                        column_name,
                    "classificationCode":
                        classification_code,
                    "emptyCount":
                        empty_count,
                    "message":
                        "Multiple data types were "
                        "detected in this column.",
                }
            )

        if classification_status == "UNCLASSIFIED":
            findings.append(
                {
                    "findingCode":
                        "UNCLASSIFIED_COLUMN",
                    "findingCategory":
                        "CLASSIFICATION",
                    "severity":
                        "WARNING",
                    "columnName":
                        column_name,
                    "classificationCode":
                        None,
                    "emptyCount":
                        empty_count,
                    "message":
                        "This column could not be "
                        "classified automatically.",
                }
            )

            if (
                match_percentage is not None
                and 0 < match_percentage < 80
            ):
                findings.append(
                    {
                        "findingCode":
                            "LOW_PATTERN_MATCH",
                        "findingCategory":
                            "CLASSIFICATION",
                        "severity":
                            "INFO",
                        "columnName":
                            column_name,
                        "classificationCode":
                            None,
                        "emptyCount":
                            empty_count,
                        "message":
                            "Some values matched a "
                            "known pattern, but the "
                            "classification threshold "
                            "was not met.",
                    }
                )

        if (
            classification_status == "CLASSIFIED"
            and classification_method
            == "VALUE_PATTERN_RULE"
        ):
            findings.append(
                {
                    "findingCode":
                        "VALUE_PATTERN_CLASSIFICATION",
                    "findingCategory":
                        "CLASSIFICATION",
                    "severity":
                        "INFO",
                    "columnName":
                        column_name,
                    "classificationCode":
                        classification_code,
                    "emptyCount":
                        empty_count,
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