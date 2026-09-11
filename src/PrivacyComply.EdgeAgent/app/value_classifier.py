import re
from typing import Any


EMAIL_PATTERN = re.compile(
    r"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$"
)

PHONE_PATTERN = re.compile(
    r"^\+?[0-9][0-9\s().-]{7,20}$"
)


def classify_values(
    values: list[Any],
) -> dict[str, Any]:
    non_empty_values = [
        str(value).strip()
        for value in values
        if value is not None
        and str(value).strip()
    ]

    if not non_empty_values:
        return {
            "classificationStatus": "UNCLASSIFIED",
            "classificationCode": None,
            "classificationMethod": "VALUE_PATTERN_RULE",
            "matchPercentage": 0.0,
        }

    email_matches = sum(
        1
        for value in non_empty_values
        if EMAIL_PATTERN.fullmatch(value)
    )

    phone_matches = sum(
        1
        for value in non_empty_values
        if PHONE_PATTERN.fullmatch(value)
    )

    value_count = len(non_empty_values)

    email_percentage = round(
        (email_matches / value_count) * 100,
        2,
    )

    phone_percentage = round(
        (phone_matches / value_count) * 100,
        2,
    )

    if email_percentage >= 80:
        return {
            "classificationStatus": "CLASSIFIED",
            "classificationCode": "EMAIL_ADDRESS",
            "classificationMethod": "VALUE_PATTERN_RULE",
            "matchPercentage": email_percentage,
        }

    if phone_percentage >= 80:
        return {
            "classificationStatus": "CLASSIFIED",
            "classificationCode": "PHONE_NUMBER",
            "classificationMethod": "VALUE_PATTERN_RULE",
            "matchPercentage": phone_percentage,
        }

    return {
        "classificationStatus": "UNCLASSIFIED",
        "classificationCode": None,
        "classificationMethod": "VALUE_PATTERN_RULE",
        "matchPercentage": max(
            email_percentage,
            phone_percentage,
        ),
    }