import re
from typing import Any


EMAIL_PATTERN = re.compile(
    r"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$"
)

PHONE_PATTERN = re.compile(
    r"^\+?[0-9][0-9\s().-]{7,20}$"
)

PAN_PATTERN = re.compile(
    r"^[A-Z]{5}[0-9]{4}[A-Z]$"
)

AADHAAR_PATTERN = re.compile(
    r"^[2-9][0-9]{11}$"
)

GSTIN_PATTERN = re.compile(
    r"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$"
)

CLASSIFICATION_THRESHOLD = 80.0

GSTIN_CHARACTERS = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"


VERHOEFF_D = [
    [0, 1, 2, 3, 4, 5, 6, 7, 8, 9],
    [1, 2, 3, 4, 0, 6, 7, 8, 9, 5],
    [2, 3, 4, 0, 1, 7, 8, 9, 5, 6],
    [3, 4, 0, 1, 2, 8, 9, 5, 6, 7],
    [4, 0, 1, 2, 3, 9, 5, 6, 7, 8],
    [5, 9, 8, 7, 6, 0, 4, 3, 2, 1],
    [6, 5, 9, 8, 7, 1, 0, 4, 3, 2],
    [7, 6, 5, 9, 8, 2, 1, 0, 4, 3],
    [8, 7, 6, 5, 9, 3, 2, 1, 0, 4],
    [9, 8, 7, 6, 5, 4, 3, 2, 1, 0],
]

VERHOEFF_P = [
    [0, 1, 2, 3, 4, 5, 6, 7, 8, 9],
    [1, 5, 7, 6, 2, 8, 3, 0, 9, 4],
    [5, 8, 0, 3, 7, 9, 6, 1, 4, 2],
    [8, 9, 1, 6, 0, 4, 3, 5, 2, 7],
    [9, 4, 5, 3, 1, 2, 6, 8, 7, 0],
    [4, 2, 8, 6, 5, 7, 3, 9, 0, 1],
    [2, 7, 9, 3, 8, 0, 6, 4, 1, 5],
    [7, 0, 4, 6, 9, 1, 3, 2, 5, 8],
]


def is_valid_aadhaar(
    value: str,
) -> bool:
    normalized_value = (
        value.replace(" ", "")
        .replace("-", "")
        .strip()
    )

    if not AADHAAR_PATTERN.fullmatch(normalized_value):
        return False

    checksum = 0

    for index, digit in enumerate(
        reversed(normalized_value)
    ):
        checksum = VERHOEFF_D[checksum][
            VERHOEFF_P[index % 8][int(digit)]
        ]

    return checksum == 0


def calculate_gstin_checksum(
    gstin_body: str,
) -> str:
    factor = 1
    total = 0

    for character in gstin_body:
        character_code = GSTIN_CHARACTERS.index(
            character
        )

        product = character_code * factor

        total += (
            product // 36
            + product % 36
        )

        factor = 2 if factor == 1 else 1

    check_code = (
        36 - (total % 36)
    ) % 36

    return GSTIN_CHARACTERS[check_code]


def is_valid_gstin(
    value: str,
) -> bool:
    normalized_value = value.strip().upper()

    if not GSTIN_PATTERN.fullmatch(normalized_value):
        return False

    expected_checksum = calculate_gstin_checksum(
        normalized_value[:14]
    )

    return normalized_value[14] == expected_checksum


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

    value_count = len(non_empty_values)

    email_matches = sum(
        1
        for value in non_empty_values
        if EMAIL_PATTERN.fullmatch(value)
    )

    pan_matches = sum(
        1
        for value in non_empty_values
        if PAN_PATTERN.fullmatch(value.upper())
    )

    aadhaar_matches = sum(
        1
        for value in non_empty_values
        if is_valid_aadhaar(value)
    )

    gstin_matches = sum(
        1
        for value in non_empty_values
        if is_valid_gstin(value)
    )

    phone_matches = sum(
        1
        for value in non_empty_values
        if PHONE_PATTERN.fullmatch(value)
        and not is_valid_aadhaar(value)
    )

    email_percentage = round(
        (email_matches / value_count) * 100,
        2,
    )

    phone_percentage = round(
        (phone_matches / value_count) * 100,
        2,
    )

    pan_percentage = round(
        (pan_matches / value_count) * 100,
        2,
    )

    aadhaar_percentage = round(
        (aadhaar_matches / value_count) * 100,
        2,
    )

    gstin_percentage = round(
        (gstin_matches / value_count) * 100,
        2,
    )

    if email_percentage >= CLASSIFICATION_THRESHOLD:
        return {
            "classificationStatus": "CLASSIFIED",
            "classificationCode": "EMAIL_ADDRESS",
            "classificationMethod": "VALUE_PATTERN_RULE",
            "matchPercentage": email_percentage,
        }

    if pan_percentage >= CLASSIFICATION_THRESHOLD:
        return {
            "classificationStatus": "CLASSIFIED",
            "classificationCode": "INDIA_PAN",
            "classificationMethod": "VALUE_PATTERN_RULE",
            "matchPercentage": pan_percentage,
        }

    if aadhaar_percentage >= CLASSIFICATION_THRESHOLD:
        return {
            "classificationStatus": "CLASSIFIED",
            "classificationCode": "INDIA_AADHAAR",
            "classificationMethod": "VALUE_PATTERN_RULE",
            "matchPercentage": aadhaar_percentage,
        }

    if gstin_percentage >= CLASSIFICATION_THRESHOLD:
        return {
            "classificationStatus": "CLASSIFIED",
            "classificationCode": "INDIA_GSTIN",
            "classificationMethod": "VALUE_PATTERN_RULE",
            "matchPercentage": gstin_percentage,
        }

    if phone_percentage >= CLASSIFICATION_THRESHOLD:
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
            pan_percentage,
            aadhaar_percentage,
            gstin_percentage,
        ),
    }