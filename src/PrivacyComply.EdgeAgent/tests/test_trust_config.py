from fastapi.testclient import TestClient

from app.main import app
from app.trust_config import (
    get_configured_trust_token,
)


client = TestClient(app)


TEST_TRUST_TOKEN = (
    "privacycomply-test-edge-token-"
    "0123456789"
)


def build_test_file() -> dict:
    return {
        "file": (
            "contacts.csv",
            (
                b"email\n"
                b"alice@example.com\n"
            ),
            "text/csv",
        )
    }


def test_configured_trust_token(
    monkeypatch,
) -> None:
    monkeypatch.setenv(
        "EDGE_TRUST_TOKEN",
        TEST_TRUST_TOKEN,
    )

    assert (
        get_configured_trust_token()
        == TEST_TRUST_TOKEN
    )


def test_missing_configuration_rejected(
    monkeypatch,
) -> None:
    monkeypatch.delenv(
        "EDGE_TRUST_TOKEN",
        raising=False,
    )

    response = client.post(
        "/analysis/files/inspect",
        files=build_test_file(),
        headers={
            "X-PrivacyComply-Edge-Token":
                TEST_TRUST_TOKEN,
        },
    )

    assert response.status_code == 503

    assert response.json() == {
        "errorCode":
            "EDGE_TRUST_NOT_CONFIGURED",
        "message": (
            "Edge Agent trust token "
            "is not configured."
        ),
        "statusCode": 503,
    }


def test_missing_request_token_rejected(
    monkeypatch,
) -> None:
    monkeypatch.setenv(
        "EDGE_TRUST_TOKEN",
        TEST_TRUST_TOKEN,
    )

    response = client.post(
        "/analysis/files/inspect",
        files=build_test_file(),
    )

    assert response.status_code == 401

    assert response.json() == {
        "errorCode":
            "EDGE_TRUST_TOKEN_REQUIRED",
        "message": (
            "Edge Agent trust token "
            "is required."
        ),
        "statusCode": 401,
    }


def test_invalid_request_token_rejected(
    monkeypatch,
) -> None:
    monkeypatch.setenv(
        "EDGE_TRUST_TOKEN",
        TEST_TRUST_TOKEN,
    )

    invalid_token = (
        "invalid-private-token-value"
    )

    response = client.post(
        "/analysis/files/inspect",
        files=build_test_file(),
        headers={
            "X-PrivacyComply-Edge-Token":
                invalid_token,
        },
    )

    assert response.status_code == 401

    payload = response.json()

    assert (
        payload["errorCode"]
        == "EDGE_TRUST_TOKEN_INVALID"
    )

    assert (
        invalid_token
        not in response.text
    )


def test_valid_request_token_accepted(
    monkeypatch,
) -> None:
    monkeypatch.setenv(
        "EDGE_TRUST_TOKEN",
        TEST_TRUST_TOKEN,
    )

    response = client.post(
        "/analysis/files/inspect",
        files=build_test_file(),
        headers={
            "X-PrivacyComply-Edge-Token":
                TEST_TRUST_TOKEN,
        },
    )

    assert response.status_code == 200

    assert (
        response.json()["fileName"]
        == "contacts.csv"
    )


def test_health_does_not_require_trust_token(
    monkeypatch,
) -> None:
    monkeypatch.delenv(
        "EDGE_TRUST_TOKEN",
        raising=False,
    )

    response = client.get(
        "/health"
    )

    assert response.status_code == 200

    assert (
        response.json()["status"]
        == "healthy"
    )