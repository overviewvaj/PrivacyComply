import pytest

from fastapi.testclient import TestClient

from app.main import app


client = TestClient(app)


TEST_TRUST_TOKEN = (
    "privacycomply-test-edge-token-"
    "0123456789"
)


@pytest.fixture(
    autouse=True
)
def configure_test_trust(
    monkeypatch,
) -> None:
    monkeypatch.setenv(
        "EDGE_TRUST_TOKEN",
        TEST_TRUST_TOKEN,
    )


def trust_headers() -> dict[str, str]:
    return {
        "X-PrivacyComply-Edge-Token":
            TEST_TRUST_TOKEN,
    }


def test_health_endpoint() -> None:
    response = client.get(
        "/health"
    )

    assert response.status_code == 200

    assert response.json() == {
        "status": "healthy",
        "service":
            "PrivacyComply.EdgeAgent",
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
        headers=trust_headers(),
    )

    assert response.status_code == 200

    payload = response.json()

    assert (
        payload["fileName"]
        == "contacts.csv"
    )

    assert (
        payload["fileExtension"]
        == ".csv"
    )

    assert (
        payload["processingLocation"]
        == "LOCAL_EDGE_AGENT"
    )

    assert payload["rowCount"] == 3
    assert payload["columnCount"] == 1

    assert (
        payload[
            "columnProfiles"
        ][0]["classificationCode"]
        == "EMAIL_ADDRESS"
    )


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
        headers=trust_headers(),
    )

    assert response.status_code == 200

    payload = response.json()

    assert (
        payload[
            "columnProfiles"
        ][0]["classificationCode"]
        == "INDIA_GSTIN"
    )

    assert (
        payload[
            "columnProfiles"
        ][0]["classificationStatus"]
        == "CLASSIFIED"
    )


def test_unsupported_file_endpoint() -> None:
    response = client.post(
        "/analysis/files/inspect",
        files={
            "file": (
                "customers.txt",
                b"name\nAlice\n",
                "text/plain",
            )
        },
        headers=trust_headers(),
    )

    assert response.status_code == 400

    assert response.json() == {
        "errorCode":
            "UNSUPPORTED_FILE_TYPE",
        "message": (
            "Unsupported file type. "
            "Only .xlsx and .csv files "
            "are supported."
        ),
        "statusCode": 400,
    }


def test_missing_file_returns_validation_error() -> None:
    response = client.post(
        "/analysis/files/inspect",
        headers=trust_headers(),
    )

    assert response.status_code == 422

    assert response.json() == {
        "errorCode":
            "FILE_REQUIRED",
        "message":
            "A file upload is required.",
        "statusCode":
            422,
    }


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
        headers=trust_headers(),
    )

    assert response.status_code == 400

    assert response.json() == {
        "errorCode":
            "CSV_DECODING_ERROR",
        "message": (
            "The CSV file could not be "
            "decoded using UTF-8 encoding."
        ),
        "statusCode":
            400,
    }