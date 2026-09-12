import json

import pytest

from fastapi.testclient import TestClient

from app.main import app


client = TestClient(app)


TEST_TRUST_TOKEN = (
    "privacycomply-contract-test-token-"
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


def test_v1_success_contract_top_level_fields() -> None:
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

    assert set(payload.keys()) == {
        "fileName",
        "fileExtension",
        "contentType",
        "fileSizeBytes",
        "processingLocation",
        "sheetName",
        "rowCount",
        "columnCount",
        "columns",
        "columnProfiles",
        "classificationSummary",
        "findingSummary",
        "findings",
    }

    assert (
        payload["processingLocation"]
        == "LOCAL_EDGE_AGENT"
    )


def test_v1_classification_summary_contract() -> None:
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

    summary = response.json()[
        "classificationSummary"
    ]

    assert set(summary.keys()) == {
        "totalColumns",
        "classifiedColumns",
        "unclassifiedColumns",
        "classificationCoveragePercentage",
    }

    assert summary["totalColumns"] == 1

    assert (
        summary[
            "classifiedColumns"
        ]
        == 1
    )


def test_v1_finding_summary_contract() -> None:
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

    summary = response.json()[
        "findingSummary"
    ]

    assert set(summary.keys()) == {
        "totalFindings",
        "infoCount",
        "warningCount",
        "errorCount",
    }


def test_v1_success_response_excludes_raw_customer_values() -> None:
    raw_customer_values = [
        "alice.sensitive@example.com",
        "bob.private@example.org",
        "+447700900001",
    ]

    content = (
        "email_address,phone_number\n"
        "alice.sensitive@example.com,"
        "+447700900001\n"
        "bob.private@example.org,"
        "+447700900001\n"
    ).encode("utf-8")

    response = client.post(
        "/analysis/files/inspect",
        files={
            "file": (
                "customer_data.csv",
                content,
                "text/csv",
            )
        },
        headers=trust_headers(),
    )

    assert response.status_code == 200

    serialized_response = json.dumps(
        response.json(),
        sort_keys=True,
    )

    for raw_value in raw_customer_values:
        assert (
            raw_value
            not in serialized_response
        )

    assert (
        TEST_TRUST_TOKEN
        not in serialized_response
    )


def test_v1_error_contract_fields() -> None:
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

    payload = response.json()

    assert set(payload.keys()) == {
        "errorCode",
        "message",
        "statusCode",
    }

    assert (
        payload["errorCode"]
        == "UNSUPPORTED_FILE_TYPE"
    )

    assert payload["statusCode"] == 400

    serialized_response = json.dumps(
        payload,
        sort_keys=True,
    )

    assert (
        TEST_TRUST_TOKEN
        not in serialized_response
    )


def test_v1_openapi_documents_protected_endpoint() -> None:
    response = client.get(
        "/openapi.json"
    )

    assert response.status_code == 200

    openapi = response.json()

    inspect_operation = (
        openapi["paths"]
        ["/analysis/files/inspect"]
        ["post"]
    )

    documented_responses = (
        inspect_operation["responses"]
    )

    assert "200" in documented_responses
    assert "400" in documented_responses
    assert "401" in documented_responses
    assert "413" in documented_responses
    assert "422" in documented_responses
    assert "503" in documented_responses