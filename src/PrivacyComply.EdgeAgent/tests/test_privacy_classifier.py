from app.privacy_classifier import (
    PrivacyCategory,
    categorize_column,
    CLASSIFICATION_TO_PRIVACY_CATEGORY,
    REGULATED_IDENTIFIERS,
)
from app.file_profiler import (
    build_column_profiles,
    build_classification_summary,
)


def test_personal_data_mappings() -> None:
    personal_codes = [
        "PERSON_NAME",
        "EMAIL_ADDRESS",
        "PHONE_NUMBER",
        "DATE_OF_BIRTH",
        "POSTAL_ADDRESS",
        "LOCATION_CITY",
        "POSTAL_CODE",
        "CUSTOMER_IDENTIFIER",
        "INDIA_PAN",
        "INDIA_AADHAAR",
        "CONSENT_INDICATOR",
        "CONSENT_DATE",
        "APPOINTMENT_DATE",
    ]

    for code in personal_codes:
        result = categorize_column(code)
        assert result["privacyCategory"] == PrivacyCategory.PERSONAL_DATA.value
        assert result["isPersonalData"] is True


def test_context_dependent_mappings() -> None:
    context_codes = [
        "RECORD_CREATED_TIMESTAMP",
        "RECORD_UPDATED_TIMESTAMP",
        "APPOINTMENT_STATUS",
        "INDIA_GSTIN",
    ]

    for code in context_codes:
        result = categorize_column(code)
        assert result["privacyCategory"] == PrivacyCategory.CONTEXT_DEPENDENT.value
        assert result["isPersonalData"] is False
        assert result["isRegulatedIdentifier"] is False


def test_regulated_identifiers() -> None:
    pan_result = categorize_column("INDIA_PAN")
    assert pan_result["privacyCategory"] == PrivacyCategory.PERSONAL_DATA.value
    assert pan_result["isPersonalData"] is True
    assert pan_result["isRegulatedIdentifier"] is True

    aadhaar_result = categorize_column("INDIA_AADHAAR")
    assert aadhaar_result["privacyCategory"] == PrivacyCategory.PERSONAL_DATA.value
    assert aadhaar_result["isPersonalData"] is True
    assert aadhaar_result["isRegulatedIdentifier"] is True

    # GSTIN is not a regulated personal identifier under DPDP
    gstin_result = categorize_column("INDIA_GSTIN")
    assert gstin_result["isRegulatedIdentifier"] is False


def test_unclassified_or_none_column() -> None:
    assert categorize_column(None) == {
        "privacyCategory": "NOT_PERSONAL",
        "isPersonalData": False,
        "isRegulatedIdentifier": False,
    }

    assert categorize_column("UNKNOWN_CODE") == {
        "privacyCategory": "NOT_PERSONAL",
        "isPersonalData": False,
        "isRegulatedIdentifier": False,
    }


def test_phase1_gate_14_classified_columns_deterministic() -> None:
    """Gate test: A dataset containing 14 classified columns must produce

    deterministic privacy categories without transmitting raw values.
    """
    # 14 distinct columns from classification rules
    columns = [
        "FullName",
        "Email",
        "Mobile",
        "DOB",
        "Address",
        "City",
        "PostalCode",
        "CustomerId",
        "PAN",
        "Aadhaar",
        "ConsentFlag",
        "AppointmentDate",
        "AppointmentStatus",
        "GSTIN",
    ]

    dummy_row = [
        "Synthetic Name",
        "synthetic@example.com",
        "9876543210",
        "1990-01-01",
        "123 Street",
        "Mumbai",
        "400001",
        "CUST-001",
        "ABCDE1234F",
        "234567890123",
        "Yes",
        "2026-09-15",
        "CONFIRMED",
        "27AAAAA0000A1Z5",
    ]

    profiles = build_column_profiles(columns, [dummy_row])
    assert len(profiles) == 14

    summary = build_classification_summary(profiles)
    assert summary["totalColumns"] == 14
    assert summary["classifiedColumns"] == 14
    assert summary["unclassifiedColumns"] == 0
    assert summary["classificationCoveragePercentage"] == 100.0

    # 12 personal data columns (FullName, Email, Mobile, DOB, Address, City, PostalCode, CustomerId, PAN, Aadhaar, ConsentFlag, AppointmentDate)
    assert summary["totalPersonalDataColumns"] == 12

    # 2 context-dependent columns (AppointmentStatus, GSTIN)
    assert summary["totalContextDependentColumns"] == 2

    # 2 regulated identifiers (PAN, Aadhaar)
    assert summary["totalRegulatedIdentifierColumns"] == 2

    # Ensure no raw values leaked in profiles
    for profile in profiles:
        assert "Synthetic Name" not in str(profile)
        assert "synthetic@example.com" not in str(profile)
        assert "ABCDE1234F" not in str(profile)
        assert "234567890123" not in str(profile)
