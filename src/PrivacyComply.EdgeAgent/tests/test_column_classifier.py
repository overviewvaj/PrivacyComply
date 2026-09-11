from app.column_classifier import (
    classify_column_name,
    normalize_column_name,
)


def test_normalize_column_name() -> None:
    assert normalize_column_name(
        "PAN Number"
    ) == "pannumber"

    assert normalize_column_name(
        "aadhaar_number"
    ) == "aadhaarnumber"

    assert normalize_column_name(
        "GST-IN"
    ) == "gstin"


def test_email_column_classification() -> None:
    result = classify_column_name(
        "Email Address"
    )

    assert result["classificationStatus"] == "CLASSIFIED"
    assert result["classificationCode"] == "EMAIL_ADDRESS"
    assert result["classificationMethod"] == "COLUMN_NAME_RULE"


def test_phone_column_classification() -> None:
    result = classify_column_name(
        "Mobile Number"
    )

    assert result["classificationStatus"] == "CLASSIFIED"
    assert result["classificationCode"] == "PHONE_NUMBER"
    assert result["classificationMethod"] == "COLUMN_NAME_RULE"


def test_pan_column_classification() -> None:
    result = classify_column_name(
        "PAN Number"
    )

    assert result["classificationStatus"] == "CLASSIFIED"
    assert result["classificationCode"] == "INDIA_PAN"
    assert result["classificationMethod"] == "COLUMN_NAME_RULE"


def test_aadhaar_column_classification() -> None:
    result = classify_column_name(
        "Aadhaar Number"
    )

    assert result["classificationStatus"] == "CLASSIFIED"
    assert result["classificationCode"] == "INDIA_AADHAAR"
    assert result["classificationMethod"] == "COLUMN_NAME_RULE"


def test_gstin_column_classification() -> None:
    result = classify_column_name(
        "GSTIN"
    )

    assert result["classificationStatus"] == "CLASSIFIED"
    assert result["classificationCode"] == "INDIA_GSTIN"
    assert result["classificationMethod"] == "COLUMN_NAME_RULE"


def test_unknown_column_is_unclassified() -> None:
    result = classify_column_name(
        "random_notes"
    )

    assert result["classificationStatus"] == "UNCLASSIFIED"
    assert result["classificationCode"] is None
    assert result["classificationMethod"] == "COLUMN_NAME_RULE"