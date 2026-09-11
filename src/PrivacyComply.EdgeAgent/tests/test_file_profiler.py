from datetime import date, datetime

from app.file_profiler import (
    build_classification_summary,
    build_column_profiles,
    infer_column_type,
    infer_value_type,
)


def test_infer_empty_values() -> None:
    assert infer_value_type(None) == "EMPTY"
    assert infer_value_type("") == "EMPTY"
    assert infer_value_type("   ") == "EMPTY"


def test_infer_boolean_value() -> None:
    assert infer_value_type(True) == "BOOLEAN"
    assert infer_value_type(False) == "BOOLEAN"


def test_infer_integer_value() -> None:
    assert infer_value_type(123) == "INTEGER"


def test_infer_decimal_value() -> None:
    assert infer_value_type(123.45) == "DECIMAL"


def test_infer_date_value() -> None:
    assert infer_value_type(
        date(2026, 9, 12)
    ) == "DATE"


def test_infer_datetime_value() -> None:
    assert infer_value_type(
        datetime(2026, 9, 12, 10, 30)
    ) == "DATETIME"


def test_infer_iso_date_string() -> None:
    assert infer_value_type(
        "2026-09-12"
    ) == "DATE"


def test_infer_iso_datetime_string() -> None:
    assert infer_value_type(
        "2026-09-12T10:30:00"
    ) == "DATETIME"


def test_infer_text_value() -> None:
    assert infer_value_type(
        "PrivacyComply"
    ) == "TEXT"


def test_empty_column_type() -> None:
    assert infer_column_type(
        [
            None,
            "",
            "   ",
        ]
    ) == "EMPTY"


def test_single_column_type() -> None:
    assert infer_column_type(
        [
            "alpha",
            "beta",
            "gamma",
        ]
    ) == "TEXT"


def test_integer_decimal_column_becomes_decimal() -> None:
    assert infer_column_type(
        [
            1,
            2.5,
            3,
        ]
    ) == "DECIMAL"


def test_mixed_column_type() -> None:
    assert infer_column_type(
        [
            1,
            "alpha",
        ]
    ) == "MIXED"


def test_column_name_classification_has_priority() -> None:
    profiles = build_column_profiles(
        columns=[
            "email_address",
        ],
        data_rows=[
            ["not-an-email"],
            ["another-invalid-value"],
            ["random-text"],
        ],
    )

    profile = profiles[0]

    assert profile["classificationStatus"] == "CLASSIFIED"
    assert profile["classificationCode"] == "EMAIL_ADDRESS"
    assert profile["classificationMethod"] == "COLUMN_NAME_RULE"
    assert profile["matchPercentage"] is None


def test_value_pattern_fallback() -> None:
    profiles = build_column_profiles(
        columns=[
            "contact_value",
        ],
        data_rows=[
            ["alice@example.com"],
            ["bob@example.org"],
            ["charlie@example.net"],
        ],
    )

    profile = profiles[0]

    assert profile["classificationStatus"] == "CLASSIFIED"
    assert profile["classificationCode"] == "EMAIL_ADDRESS"
    assert profile["classificationMethod"] == "VALUE_PATTERN_RULE"
    assert profile["matchPercentage"] == 100.0


def test_unclassified_column() -> None:
    profiles = build_column_profiles(
        columns=[
            "random_notes",
        ],
        data_rows=[
            ["alpha"],
            ["beta"],
            ["gamma"],
        ],
    )

    profile = profiles[0]

    assert profile["classificationStatus"] == "UNCLASSIFIED"
    assert profile["classificationCode"] is None
    assert profile["classificationMethod"] == "NO_MATCH"
    assert profile["matchPercentage"] == 0.0


def test_empty_and_non_empty_counts() -> None:
    profiles = build_column_profiles(
        columns=[
            "contact_value",
        ],
        data_rows=[
            ["alice@example.com"],
            [""],
            ["bob@example.org"],
            [None],
        ],
    )

    profile = profiles[0]

    assert profile["nonEmptyCount"] == 2
    assert profile["emptyCount"] == 2


def test_multiple_columns_are_profiled_independently() -> None:
    profiles = build_column_profiles(
        columns=[
            "contact_value",
            "primary_contact",
            "notes",
        ],
        data_rows=[
            [
                "alice@example.com",
                "+447700900001",
                "alpha",
            ],
            [
                "bob@example.org",
                "+447700900002",
                "beta",
            ],
            [
                "charlie@example.net",
                "+447700900003",
                "gamma",
            ],
        ],
    )

    assert len(profiles) == 3

    assert profiles[0]["classificationCode"] == "EMAIL_ADDRESS"
    assert profiles[1]["classificationCode"] == "PHONE_NUMBER"
    assert profiles[2]["classificationStatus"] == "UNCLASSIFIED"


def test_classification_summary() -> None:
    profiles = [
        {
            "classificationStatus": "CLASSIFIED",
        },
        {
            "classificationStatus": "CLASSIFIED",
        },
        {
            "classificationStatus": "UNCLASSIFIED",
        },
    ]

    summary = build_classification_summary(
        profiles
    )

    assert summary["totalColumns"] == 3
    assert summary["classifiedColumns"] == 2
    assert summary["unclassifiedColumns"] == 1
    assert (
        summary["classificationCoveragePercentage"]
        == 66.67
    )


def test_empty_classification_summary() -> None:
    summary = build_classification_summary([])

    assert summary == {
        "totalColumns": 0,
        "classifiedColumns": 0,
        "unclassifiedColumns": 0,
        "classificationCoveragePercentage": 0.0,
    }
