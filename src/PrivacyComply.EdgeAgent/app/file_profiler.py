from datetime import date, datetime
from typing import Any

from app.column_classifier import classify_column_name
from app.privacy_classifier import categorize_column
from app.value_classifier import classify_values


def infer_value_type(
    value: Any,
) -> str:
    if value is None:
        return "EMPTY"

    if isinstance(value, bool):
        return "BOOLEAN"

    if isinstance(value, datetime):
        return "DATETIME"

    if isinstance(value, date):
        return "DATE"

    if isinstance(value, int):
        return "INTEGER"

    if isinstance(value, float):
        return "DECIMAL"

    text_value = str(value).strip()

    if not text_value:
        return "EMPTY"

    try:
        datetime.fromisoformat(
            text_value.replace(
                "Z",
                "+00:00",
            )
        )

        if (
            "T" in text_value
            or " " in text_value
        ):
            return "DATETIME"

        if len(text_value) == 10:
            return "DATE"

        return "DATETIME"

    except ValueError:
        return "TEXT"


def infer_column_type(
    values: list[Any],
) -> str:
    detected_types = {
        infer_value_type(value)
        for value in values
        if infer_value_type(value) != "EMPTY"
    }

    if not detected_types:
        return "EMPTY"

    if len(detected_types) == 1:
        return next(iter(detected_types))

    if detected_types.issubset(
        {
            "INTEGER",
            "DECIMAL",
        }
    ):
        return "DECIMAL"

    return "MIXED"


def build_column_profiles(
    columns: list[str],
    data_rows: list[list[Any]],
) -> list[dict[str, Any]]:
    profiles: list[dict[str, Any]] = []

    for column_index, column_name in enumerate(columns):
        values: list[Any] = []

        for row in data_rows:
            value = (
                row[column_index]
                if column_index < len(row)
                else None
            )

            values.append(value)

        empty_count = sum(
            1
            for value in values
            if infer_value_type(value) == "EMPTY"
        )

        non_empty_count = (
            len(values) - empty_count
        )

        classification = classify_column_name(
            column_name
        )
        if (
            classification["classificationStatus"]
            == "UNCLASSIFIED"
        ):
            value_classification = classify_values(
                values
            )

            if (
                value_classification[
                    "classificationStatus"
                ]
                == "CLASSIFIED"
            ):
                classification = (
                    value_classification
                )
            else:
                classification = {
                    "classificationStatus":
                        "UNCLASSIFIED",
                    "classificationCode": None,
                    "classificationMethod":
                        "NO_MATCH",
                    "matchPercentage":
                        value_classification.get(
                            "matchPercentage"
                        ),
                }

        privacy = categorize_column(
            classification.get("classificationCode")
        )

        profiles.append(
            {
                "columnName": column_name,
                "ordinalPosition": column_index + 1,
                "nonEmptyCount": non_empty_count,
                "emptyCount": empty_count,
                "inferredDataType":
                    infer_column_type(values),
                "classificationStatus":
                    classification[
                        "classificationStatus"
                    ],
                "classificationCode":
                    classification[
                        "classificationCode"
                    ],
                "classificationMethod":
                    classification[
                        "classificationMethod"
                    ],
                "matchPercentage":
                    classification.get(
                        "matchPercentage"
                    ),
                "privacyCategory":
                    privacy["privacyCategory"],
                "isPersonalData":
                    privacy["isPersonalData"],
                "isRegulatedIdentifier":
                    privacy["isRegulatedIdentifier"],
            }
        )

    return profiles


def build_classification_summary(
    column_profiles: list[dict[str, Any]],
) -> dict[str, Any]:
    total_columns = len(column_profiles)

    classified_columns = sum(
        1
        for profile in column_profiles
        if profile["classificationStatus"]
        == "CLASSIFIED"
    )

    unclassified_columns = (
        total_columns - classified_columns
    )

    classification_coverage_percentage = (
        round(
            (
                classified_columns
                / total_columns
            )
            * 100,
            2,
        )
        if total_columns > 0
        else 0.0
    )

    total_personal_data_columns = sum(
        1
        for profile in column_profiles
        if profile.get("isPersonalData", False)
    )

    total_context_dependent_columns = sum(
        1
        for profile in column_profiles
        if profile.get("privacyCategory")
        == "CONTEXT_DEPENDENT"
    )

    total_regulated_identifier_columns = sum(
        1
        for profile in column_profiles
        if profile.get("isRegulatedIdentifier", False)
    )

    return {
        "totalColumns": total_columns,
        "classifiedColumns": classified_columns,
        "unclassifiedColumns": unclassified_columns,
        "classificationCoveragePercentage":
            classification_coverage_percentage,
        "totalPersonalDataColumns":
            total_personal_data_columns,
        "totalContextDependentColumns":
            total_context_dependent_columns,
        "totalRegulatedIdentifierColumns":
            total_regulated_identifier_columns,
    }
