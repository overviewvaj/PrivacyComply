from fastapi.testclient import TestClient

from app.main import app


client = TestClient(app)


def test_health_endpoint() -> None:
    response = client.get(
        "/health"
    )

    assert response.status_code == 200

    assert response.json() == {
        "status": "healthy",
        "service": "PrivacyComply.EdgeAgent",
    }


def test_inspect_csv_endpoint() -> None:
    content = (
        "contact_value\n"
        "alice@example.com\n"
        "bob@example.org\n"
        "charlie@example.net\n"
    ).encode("utf-8")

    response = client.post(
        "/analysis/files/inspect",
        files={
            "file": (
                "contacts.csv",
                content,
                "text/csv",
            )
        },
    )

    assert response.status_code == 200

    result = response.json()

    assert result["fileName"] == "contacts.csv"
    assert result["fileExtension"] == ".csv"
    assert result["processingLocation"] == "LOCAL_EDGE_AGENT"
    assert result["rowCount"] == 3
    assert result["columnCount"] == 1

    profile = result["columnProfiles"][0]

    assert profile["classificationStatus"] == "CLASSIFIED"
    assert profile["classificationCode"] == "EMAIL_ADDRESS"
    assert profile["classificationMethod"] == "VALUE_PATTERN_RULE"
    assert profile["matchPercentage"] == 100.0


def test_inspect_gstin_endpoint() -> None:
    content = (
        "tax_identifier\n"
        "27ABCDE1234F1Z0\n"
        "29PQRSX5678K1ZU\n"
        "07AAAAA1111A1ZY\n"
    ).encode("utf-8")

    response = client.post(
        "/analysis/files/inspect",
        files={
            "file": (
                "gstin.csv",
                content,
                "text/csv",
            )
        },
    )

    assert response.status_code == 200

    result = response.json()

    profile = result["columnProfiles"][0]

    assert profile["classificationStatus"] == "CLASSIFIED"
    assert profile["classificationCode"] == "INDIA_GSTIN"
    assert profile["classificationMethod"] == "VALUE_PATTERN_RULE"
    assert profile["matchPercentage"] == 100.0


def test_unsupported_file_endpoint() -> None:
    response = client.post(
        "/analysis/files/inspect",
        files={
            "file": (
                "customers.txt",
                b"test",
                "text/plain",
            )
        },
    )

    assert response.status_code == 400

    assert response.json() == {
        "detail": (
            "Unsupported file type. "
            "Only .xlsx and .csv files are supported."
        )
    }


def test_missing_file_returns_validation_error() -> None:
    response = client.post(
        "/analysis/files/inspect"
    )

    assert response.status_code == 422


def test_invalid_utf8_csv_endpoint() -> None:
    response = client.post(
        "/analysis/files/inspect",
        files={
            "file": (
                "invalid.csv",
                b"\xff\xfe\xfa",
                "text/csv",
            )
        },
    )

    assert response.status_code == 400

    assert response.json() == {
        "detail": (
            "The CSV file could not be decoded "
            "using UTF-8 encoding."
        )
    }