from app.finding_builder import (
    build_finding_summary,
    build_findings,
)


def create_profile(
    column_name: str = "TestColumn",
    non_empty_count: int = 3,
    empty_count: int = 0,
    inferred_data_type: str = "TEXT",
    classification_status: str = "CLASSIFIED",
    classification_code: str | None = "EMAIL_ADDRESS",
    classification_method: str = "COLUMN_NAME_RULE",
    match_percentage: float | None = None,
) -> dict:
    return {
        "columnName": column_name,
        "ordinalPosition": 1,
        "nonEmptyCount": non_empty_count,
        "emptyCount": empty_count,
        "inferredDataType": inferred_data_type,
        "classificationStatus": classification_status,
        "classificationCode": classification_code,
        "classificationMethod": classification_method,
        "matchPercentage": match_percentage,
    }


def test_missing_values_finding() -> None:
    profiles = [
        create_profile(
            empty_count=2,
            non_empty_count=3,
        )
    ]

    findings = build_findings(profiles)

    assert len(findings) == 1

    assert (
        findings[0]["findingCode"]
        == "MISSING_VALUES_DETECTED"
    )

    assert (
        findings[0]["findingCategory"]
        == "DATA_QUALITY"
    )

    assert findings[0]["severity"] == "INFO"


def test_empty_column_finding() -> None:
    profiles = [
        create_profile(
            non_empty_count=0,
            empty_count=5,
            inferred_data_type="EMPTY",
            classification_status="UNCLASSIFIED",
            classification_code=None,
            classification_method="NO_MATCH",
            match_percentage=0.0,
        )
    ]

    findings = build_findings(profiles)

    finding_codes = {
        finding["findingCode"]
        for finding in findings
    }

    assert "EMPTY_COLUMN" in finding_codes
    assert "UNCLASSIFIED_COLUMN" in finding_codes

    assert (
        "MISSING_VALUES_DETECTED"
        not in finding_codes
    )


def test_blank_column_name_finding() -> None:
    profiles = [
        create_profile(
            column_name="",
            classification_status="UNCLASSIFIED",
            classification_code=None,
            classification_method="NO_MATCH",
            match_percentage=0.0,
        )
    ]

    findings = build_findings(profiles)

    finding_codes = {
        finding["findingCode"]
        for finding in findings
    }

    assert "BLANK_COLUMN_NAME" in finding_codes


def test_duplicate_column_name_finding() -> None:
    profiles = [
        create_profile(
            column_name="Email",
        ),
        create_profile(
            column_name="email",
        ),
    ]

    findings = build_findings(profiles)

    duplicate_findings = [
        finding
        for finding in findings
        if finding["findingCode"]
        == "DUPLICATE_COLUMN_NAME"
    ]

    assert len(duplicate_findings) == 1


def test_mixed_data_types_finding() -> None:
    profiles = [
        create_profile(
            inferred_data_type="MIXED",
        )
    ]

    findings = build_findings(profiles)

    assert len(findings) == 1

    assert (
        findings[0]["findingCode"]
        == "MIXED_DATA_TYPES"
    )

    assert (
        findings[0]["findingCategory"]
        == "DATA_QUALITY"
    )


def test_unclassified_column_finding() -> None:
    profiles = [
        create_profile(
            classification_status="UNCLASSIFIED",
            classification_code=None,
            classification_method="NO_MATCH",
            match_percentage=0.0,
        )
    ]

    findings = build_findings(profiles)

    assert len(findings) == 1

    assert (
        findings[0]["findingCode"]
        == "UNCLASSIFIED_COLUMN"
    )

    assert (
        findings[0]["findingCategory"]
        == "CLASSIFICATION"
    )

    assert findings[0]["severity"] == "WARNING"


def test_low_pattern_match_finding() -> None:
    profiles = [
        create_profile(
            classification_status="UNCLASSIFIED",
            classification_code=None,
            classification_method="NO_MATCH",
            match_percentage=66.67,
        )
    ]

    findings = build_findings(profiles)

    finding_codes = {
        finding["findingCode"]
        for finding in findings
    }

    assert "UNCLASSIFIED_COLUMN" in finding_codes
    assert "LOW_PATTERN_MATCH" in finding_codes


def test_zero_pattern_match_has_no_low_match_finding() -> None:
    profiles = [
        create_profile(
            classification_status="UNCLASSIFIED",
            classification_code=None,
            classification_method="NO_MATCH",
            match_percentage=0.0,
        )
    ]

    findings = build_findings(profiles)

    finding_codes = {
        finding["findingCode"]
        for finding in findings
    }

    assert "UNCLASSIFIED_COLUMN" in finding_codes

    assert (
        "LOW_PATTERN_MATCH"
        not in finding_codes
    )


def test_value_pattern_classification_finding() -> None:
    profiles = [
        create_profile(
            classification_status="CLASSIFIED",
            classification_code="EMAIL_ADDRESS",
            classification_method="VALUE_PATTERN_RULE",
            match_percentage=100.0,
        )
    ]

    findings = build_findings(profiles)

    assert len(findings) == 1

    assert (
        findings[0]["findingCode"]
        == "VALUE_PATTERN_CLASSIFICATION"
    )

    assert (
        findings[0]["findingCategory"]
        == "CLASSIFICATION"
    )


def test_column_name_classification_has_no_finding() -> None:
    profiles = [
        create_profile(
            classification_method="COLUMN_NAME_RULE",
        )
    ]

    findings = build_findings(profiles)

    assert findings == []


def test_multiple_findings_for_same_column() -> None:
    profiles = [
        create_profile(
            empty_count=2,
            non_empty_count=3,
            inferred_data_type="MIXED",
            classification_status="UNCLASSIFIED",
            classification_code=None,
            classification_method="NO_MATCH",
            match_percentage=50.0,
        )
    ]

    findings = build_findings(profiles)

    finding_codes = {
        finding["findingCode"]
        for finding in findings
    }

    assert finding_codes == {
        "MISSING_VALUES_DETECTED",
        "MIXED_DATA_TYPES",
        "UNCLASSIFIED_COLUMN",
        "LOW_PATTERN_MATCH",
    }


def test_finding_summary() -> None:
    findings = [
        {
            "severity": "INFO",
        },
        {
            "severity": "INFO",
        },
        {
            "severity": "WARNING",
        },
        {
            "severity": "ERROR",
        },
    ]

    summary = build_finding_summary(
        findings
    )

    assert summary == {
        "totalFindings": 4,
        "infoCount": 2,
        "warningCount": 1,
        "errorCount": 1,
    }


def test_empty_finding_summary() -> None:
    summary = build_finding_summary([])

    assert summary == {
        "totalFindings": 0,
        "infoCount": 0,
        "warningCount": 0,
        "errorCount": 0,
    }