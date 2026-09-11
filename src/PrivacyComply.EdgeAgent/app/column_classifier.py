from typing import Any


COLUMN_CLASSIFICATION_RULES = {
    # Personal identity
    "fullname": "PERSON_NAME",
    "name": "PERSON_NAME",

    # Contact information
    "email": "EMAIL_ADDRESS",
    "emailaddress": "EMAIL_ADDRESS",

    "phone": "PHONE_NUMBER",
    "phonenumber": "PHONE_NUMBER",
    "mobile": "PHONE_NUMBER",
    "mobilenumber": "PHONE_NUMBER",

    # Date of birth
    "dateofbirth": "DATE_OF_BIRTH",
    "dob": "DATE_OF_BIRTH",

    # Address and location
    "address": "POSTAL_ADDRESS",
    "addressline1": "POSTAL_ADDRESS",
    "addressline2": "POSTAL_ADDRESS",

    "city": "LOCATION_CITY",

    "postalcode": "POSTAL_CODE",
    "postcode": "POSTAL_CODE",

    # Internal identifiers
    "customerid": "CUSTOMER_IDENTIFIER",
    "customeridentifier": "CUSTOMER_IDENTIFIER",
    "clientid": "CUSTOMER_IDENTIFIER",
    "clientidentifier": "CUSTOMER_IDENTIFIER",

    # Indian identifiers
    "pan": "INDIA_PAN",
    "pannumber": "INDIA_PAN",
    "pancard": "INDIA_PAN",
    "pancardnumber": "INDIA_PAN",

    "aadhaar": "INDIA_AADHAAR",
    "aadhaarnumber": "INDIA_AADHAAR",
    "aadhaarno": "INDIA_AADHAAR",
    "aadhar": "INDIA_AADHAAR",
    "aadharnumber": "INDIA_AADHAAR",

    "gstin": "INDIA_GSTIN",
    "gstnumber": "INDIA_GSTIN",
    "gstinumber": "INDIA_GSTIN",

    # Consent
    "consentflag": "CONSENT_INDICATOR",
    "consentdate": "CONSENT_DATE",

    # Record lifecycle
    "recordcreateddate": "RECORD_CREATED_TIMESTAMP",
    "recordupdateddate": "RECORD_UPDATED_TIMESTAMP",

    # Domain-specific operational data
    "appointmentdate": "APPOINTMENT_DATE",
    "appointmentstatus": "APPOINTMENT_STATUS",
}


def normalize_column_name(
    column_name: str,
) -> str:
    return "".join(
        character.lower()
        for character in column_name
        if character.isalnum()
    )


def classify_column_name(
    column_name: str,
) -> dict[str, Any]:
    normalized_name = normalize_column_name(
        column_name
    )

    classification_code = (
        COLUMN_CLASSIFICATION_RULES.get(
            normalized_name
        )
    )

    if classification_code is None:
        return {
            "classificationStatus": "UNCLASSIFIED",
            "classificationCode": None,
            "classificationMethod": "COLUMN_NAME_RULE",
        }

    return {
        "classificationStatus": "CLASSIFIED",
        "classificationCode": classification_code,
        "classificationMethod": "COLUMN_NAME_RULE",
    }