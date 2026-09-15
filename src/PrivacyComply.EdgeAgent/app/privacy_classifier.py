from enum import Enum
from typing import Any


class PrivacyCategory(str, Enum):
    PERSONAL_DATA = "PERSONAL_DATA"
    CONTEXT_DEPENDENT = "CONTEXT_DEPENDENT"
    NOT_PERSONAL = "NOT_PERSONAL"


# Statutory DPDP 2023 alignment:
# Personal data is defined as any data about an individual who is identifiable by or in relation to such data.
# There is NO statutory category for "sensitive personal data" under India's DPDP Act 2023.
# High-impact identifiers (PAN, Aadhaar) are classified as PERSONAL_DATA with product-defined isRegulatedIdentifier flag.

CLASSIFICATION_TO_PRIVACY_CATEGORY: dict[str, PrivacyCategory] = {
    # Direct and indirect personal identifiers
    "PERSON_NAME": PrivacyCategory.PERSONAL_DATA,
    "EMAIL_ADDRESS": PrivacyCategory.PERSONAL_DATA,
    "PHONE_NUMBER": PrivacyCategory.PERSONAL_DATA,
    "DATE_OF_BIRTH": PrivacyCategory.PERSONAL_DATA,
    "POSTAL_ADDRESS": PrivacyCategory.PERSONAL_DATA,
    "LOCATION_CITY": PrivacyCategory.PERSONAL_DATA,
    "POSTAL_CODE": PrivacyCategory.PERSONAL_DATA,
    "CUSTOMER_IDENTIFIER": PrivacyCategory.PERSONAL_DATA,
    "INDIA_PAN": PrivacyCategory.PERSONAL_DATA,
    "INDIA_AADHAAR": PrivacyCategory.PERSONAL_DATA,
    "CONSENT_INDICATOR": PrivacyCategory.PERSONAL_DATA,
    "CONSENT_DATE": PrivacyCategory.PERSONAL_DATA,
    "APPOINTMENT_DATE": PrivacyCategory.PERSONAL_DATA,

    # Context-dependent fields (identifiability depends on combination with other records)
    "RECORD_CREATED_TIMESTAMP": PrivacyCategory.CONTEXT_DEPENDENT,
    "RECORD_UPDATED_TIMESTAMP": PrivacyCategory.CONTEXT_DEPENDENT,
    "APPOINTMENT_STATUS": PrivacyCategory.CONTEXT_DEPENDENT,
    "INDIA_GSTIN": PrivacyCategory.CONTEXT_DEPENDENT,
}

REGULATED_IDENTIFIERS: set[str] = {
    "INDIA_PAN",
    "INDIA_AADHAAR",
}


def categorize_column(
    classification_code: str | None,
) -> dict[str, Any]:
    if not classification_code:
        return {
            "privacyCategory": PrivacyCategory.NOT_PERSONAL.value,
            "isPersonalData": False,
            "isRegulatedIdentifier": False,
        }

    category = CLASSIFICATION_TO_PRIVACY_CATEGORY.get(
        classification_code,
        PrivacyCategory.NOT_PERSONAL,
    )

    is_personal = category == PrivacyCategory.PERSONAL_DATA
    is_regulated = classification_code in REGULATED_IDENTIFIERS

    return {
        "privacyCategory": category.value,
        "isPersonalData": is_personal,
        "isRegulatedIdentifier": is_regulated,
    }
