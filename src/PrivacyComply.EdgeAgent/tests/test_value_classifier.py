from app.value_classifier import classify_values


def test_email_classification() -> None:
    result = classify_values(
        [
            "alice@example.com",
            "bob@example.org",
            "charlie@example.net",
        ]
    )

    assert result["classificationStatus"] == "CLASSIFIED"
    assert result["classificationCode"] == "EMAIL_ADDRESS"
    assert result["classificationMethod"] == "VALUE_PATTERN_RULE"
    assert result["matchPercentage"] == 100.0


def test_phone_classification() -> None:
    result = classify_values(
        [
            "+447700900001",
            "+447700900002",
            "+447700900003",
        ]
    )

    assert result["classificationStatus"] == "CLASSIFIED"
    assert result["classificationCode"] == "PHONE_NUMBER"
    assert result["classificationMethod"] == "VALUE_PATTERN_RULE"
    assert result["matchPercentage"] == 100.0


def test_pan_classification() -> None:
    result = classify_values(
        [
            "ABCDE1234F",
            "PQRSX5678K",
            "AAAAA1111A",
        ]
    )

    assert result["classificationStatus"] == "CLASSIFIED"
    assert result["classificationCode"] == "INDIA_PAN"
    assert result["classificationMethod"] == "VALUE_PATTERN_RULE"
    assert result["matchPercentage"] == 100.0


def test_aadhaar_classification() -> None:
    result = classify_values(
        [
            "234567890124",
            "345678901238",
            "456789012341",
        ]
    )

    assert result["classificationStatus"] == "CLASSIFIED"
    assert result["classificationCode"] == "INDIA_AADHAAR"
    assert result["classificationMethod"] == "VALUE_PATTERN_RULE"
    assert result["matchPercentage"] == 100.0


def test_invalid_aadhaar_is_not_classified_as_aadhaar() -> None:
    result = classify_values(
        [
            "234567890123",
            "345678901234",
            "456789012345",
        ]
    )

    assert result["classificationCode"] != "INDIA_AADHAAR"


def test_gstin_classification() -> None:
    result = classify_values(
        [
            "27ABCDE1234F1Z0",
            "29PQRSX5678K1ZU",
            "07AAAAA1111A1ZY",
        ]
    )

    assert result["classificationStatus"] == "CLASSIFIED"
    assert result["classificationCode"] == "INDIA_GSTIN"
    assert result["classificationMethod"] == "VALUE_PATTERN_RULE"
    assert result["matchPercentage"] == 100.0


def test_invalid_gstin_checksum_is_rejected() -> None:
    result = classify_values(
        [
            "27ABCDE1234F1Z5",
            "29PQRSX5678K1Z2",
            "07AAAAA1111A1Z9",
        ]
    )

    assert result["classificationStatus"] == "UNCLASSIFIED"
    assert result["classificationCode"] is None
    assert result["matchPercentage"] == 0.0


def test_exact_eighty_percent_threshold() -> None:
    result = classify_values(
        [
            "alice@example.com",
            "bob@example.org",
            "charlie@example.net",
            "david@example.co.uk",
            "not-an-email",
        ]
    )

    assert result["classificationStatus"] == "CLASSIFIED"
    assert result["classificationCode"] == "EMAIL_ADDRESS"
    assert result["matchPercentage"] == 80.0


def test_below_threshold_is_unclassified() -> None:
    result = classify_values(
        [
            "alice@example.com",
            "bob@example.org",
            "not-an-email",
        ]
    )

    assert result["classificationStatus"] == "UNCLASSIFIED"
    assert result["classificationCode"] is None
    assert result["matchPercentage"] == 66.67


def test_empty_values_are_excluded_from_denominator() -> None:
    result = classify_values(
        [
            "alice@example.com",
            "",
            None,
            "bob@example.org",
            "   ",
            "charlie@example.net",
        ]
    )

    assert result["classificationStatus"] == "CLASSIFIED"
    assert result["classificationCode"] == "EMAIL_ADDRESS"
    assert result["matchPercentage"] == 100.0


def test_all_empty_values_are_unclassified() -> None:
    result = classify_values(
        [
            "",
            None,
            "   ",
        ]
    )

    assert result["classificationStatus"] == "UNCLASSIFIED"
    assert result["classificationCode"] is None
    assert result["matchPercentage"] == 0.0