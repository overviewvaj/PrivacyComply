from app.finding_builder import (
    build_findings,
    build_finding_summary,
)


def test_missing_values_finding() -> None:
    profiles = [
        {
            "columnName": "email",
            "emptyCount": 2,
            "classificationStatus": "CLASSIFIED",
            "classificationCode": "EMAIL_ADDRESS",
            "classificationMethod": "COLUMN_NAME_RULE",
        }
    ]

    findings = build_findings(profiles)

    assert len(findings) == 1
    assert findings[0]["findingCode"] == "MISSING_VALUES_DETECTED"
    assert findings[0]["severity"] == "INFO"
    assert findings[0]["columnName"] == "email"
    assert findings[0]["classificationCode"] == "EMAIL_ADDRESS"
    assert findings[0]["emptyCount"] == 2


def test_unclassified_column_finding() -> None:
    profiles = [
        {
            "columnName": "random_notes",
            "emptyCount": 0,
            "classificationStatus": "UNCLASSIFIED",
            "classificationCode": None,
            "classificationMethod": "NO_MATCH",
        }
    ]

    findings = build_findings(profiles)

    assert len(findings) == 1
    assert findings[0]["findingCode"] == "UNCLASSIFIED_COLUMN"
    assert findings[0]["severity"] == "WARNING"
    assert findings[0]["columnName"] == "random_notes"
    assert findings[0]["classificationCode"] is None


def test_value_pattern_classification_finding() -> None:
    profiles = [
        {
            "columnName": "identity_value",
            "emptyCount": 0,
            "classificationStatus": "CLASSIFIED",
            "classificationCode": "INDIA_PAN",
            "classificationMethod": "VALUE_PATTERN_RULE",
        }
    ]

    findings = build_findings(profiles)

    assert len(findings) == 1
    assert findings[0]["findingCode"] == "VALUE_PATTERN_CLASSIFICATION"
    assert findings[0]["severity"] == "INFO"
    assert findings[0]["classificationCode"] == "INDIA_PAN"


def test_column_name_classification_has_no_classification_finding() -> None:
    profiles = [
        {
            "columnName": "PAN Number",
            "emptyCount": 0,
            "classificationStatus": "CLASSIFIED",
            "classificationCode": "INDIA_PAN",
            "classificationMethod": "COLUMN_NAME_RULE",
        }
    ]

    findings = build_findings(profiles)

    assert findings == []


def test_multiple_findings_for_same_column() -> None:
    profiles = [
        {
            "columnName": "contact_value",
            "emptyCount": 2,
            "classificationStatus": "CLASSIFIED",
            "classificationCode": "EMAIL_ADDRESS",
            "classificationMethod": "VALUE_PATTERN_RULE",
        }
    ]

    findings = build_findings(profiles)

    assert len(findings) == 2

    finding_codes = {
        finding["findingCode"]
        for finding in findings
    }

    assert finding_codes == {
        "MISSING_VALUES_DETECTED",
        "VALUE_PATTERN_CLASSIFICATION",
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

    summary = build_finding_summary(findings)

    assert summary["totalFindings"] == 4
    assert summary["infoCount"] == 2
    assert summary["warningCount"] == 1
    assert summary["errorCount"] == 1


def test_empty_finding_summary() -> None:
    summary = build_finding_summary([])

    assert summary == {
        "totalFindings": 0,
        "infoCount": 0,
        "warningCount": 0,
        "errorCount": 0,
    }