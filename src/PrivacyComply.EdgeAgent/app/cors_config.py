import os

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware


DEFAULT_ALLOWED_ORIGINS = (
    "http://localhost:5173",
    "http://127.0.0.1:5173",
)


def get_allowed_origins() -> list[str]:
    configured_origins = os.getenv(
        "EDGE_CORS_ALLOWED_ORIGINS",
        "",
    ).strip()

    if not configured_origins:
        return list(
            DEFAULT_ALLOWED_ORIGINS
        )

    origins = [
        origin.strip()
        for origin
        in configured_origins.split(",")
        if origin.strip()
    ]

    if "*" in origins:
        raise RuntimeError(
            "EDGE_CORS_ALLOWED_ORIGINS "
            "must not contain '*'."
        )

    if not origins:
        return list(
            DEFAULT_ALLOWED_ORIGINS
        )

    return origins


def configure_cors(
    app: FastAPI,
) -> None:
    app.add_middleware(
        CORSMiddleware,
        allow_origins=get_allowed_origins(),
        allow_credentials=False,
        allow_methods=[
            "GET",
            "POST",
            "OPTIONS",
        ],
        allow_headers=[
            "Accept",
            "Content-Type",
            (
                "X-PrivacyComply-"
                "Edge-Token"
            ),
        ],
    )