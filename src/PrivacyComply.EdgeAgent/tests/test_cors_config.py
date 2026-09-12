import pytest

from fastapi.testclient import TestClient

from app.cors_config import (
    DEFAULT_ALLOWED_ORIGINS,
    get_allowed_origins,
)

from app.main import app


client = TestClient(app)


def test_default_allowed_origins(
    monkeypatch,
) -> None:
    monkeypatch.delenv(
        "EDGE_CORS_ALLOWED_ORIGINS",
        raising=False,
    )

    assert (
        get_allowed_origins()
        == list(
            DEFAULT_ALLOWED_ORIGINS
        )
    )


def test_configured_allowed_origins(
    monkeypatch,
) -> None:
    monkeypatch.setenv(
        "EDGE_CORS_ALLOWED_ORIGINS",
        (
            "http://localhost:3000,"
            "http://127.0.0.1:3000"
        ),
    )

    assert get_allowed_origins() == [
        "http://localhost:3000",
        "http://127.0.0.1:3000",
    ]


def test_wildcard_origin_rejected(
    monkeypatch,
) -> None:
    monkeypatch.setenv(
        "EDGE_CORS_ALLOWED_ORIGINS",
        "*",
    )

    with pytest.raises(
        RuntimeError,
        match="must not contain",
    ):
        get_allowed_origins()


def test_allowed_origin_receives_cors_header() -> None:
    response = client.get(
        "/health",
        headers={
            "Origin":
                "http://localhost:5173",
        },
    )

    assert response.status_code == 200

    assert (
        response.headers[
            "access-control-allow-origin"
        ]
        == "http://localhost:5173"
    )


def test_disallowed_origin_receives_no_cors_header() -> None:
    response = client.get(
        "/health",
        headers={
            "Origin":
                "https://example.com",
        },
    )

    assert response.status_code == 200

    assert (
        "access-control-allow-origin"
        not in response.headers
    )


def test_preflight_request_for_file_inspection() -> None:
    response = client.options(
        "/analysis/files/inspect",
        headers={
            "Origin":
                "http://localhost:5173",
            "Access-Control-Request-Method":
                "POST",
            "Access-Control-Request-Headers": (
                "content-type,"
                "x-privacycomply-edge-token"
            )
        },
    )

    assert response.status_code == 200

    assert (
        response.headers[
            "access-control-allow-origin"
        ]
        == "http://localhost:5173"
    )

    assert (
        "POST"
        in response.headers[
            "access-control-allow-methods"
        ]
    )