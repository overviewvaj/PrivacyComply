import os
import secrets

from fastapi import Header, HTTPException


TRUST_TOKEN_ENVIRONMENT_VARIABLE = (
    "EDGE_TRUST_TOKEN"
)


def get_configured_trust_token() -> str | None:
    configured_token = os.getenv(
        TRUST_TOKEN_ENVIRONMENT_VARIABLE
    )

    if configured_token is None:
        return None

    configured_token = (
        configured_token.strip()
    )

    if not configured_token:
        return None

    return configured_token


def verify_edge_trust(
    edge_token: str | None = Header(
        default=None,
        alias=(
            "X-PrivacyComply-Edge-Token"
        ),
    ),
) -> None:
    configured_token = (
        get_configured_trust_token()
    )

    if configured_token is None:
        raise HTTPException(
            status_code=503,
            detail=(
                "Edge Agent trust token "
                "is not configured."
            ),
        )

    if edge_token is None:
        raise HTTPException(
            status_code=401,
            detail=(
                "Edge Agent trust token "
                "is required."
            ),
        )

    if not secrets.compare_digest(
        edge_token,
        configured_token,
    ):
        raise HTTPException(
            status_code=401,
            detail=(
                "Edge Agent trust token "
                "is invalid."
            ),
        )