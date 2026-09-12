import json
import logging
import os
from datetime import datetime, timezone
from typing import Any


LOGGER_NAME = "PrivacyComply.EdgeAgent"

DEFAULT_LOG_LEVEL = "INFO"

ALLOWED_LOG_FIELDS = (
    "event",
    "path",
    "fileName",
    "fileExtension",
    "contentType",
    "fileSizeBytes",
    "rowCount",
    "columnCount",
    "classifiedColumns",
    "totalFindings",
    "statusCode",
    "errorCode",
)


class JsonLogFormatter(
    logging.Formatter
):
    def format(
        self,
        record: logging.LogRecord,
    ) -> str:
        payload: dict[str, Any] = {
            "timestamp": datetime.now(
                timezone.utc
            ).isoformat(),
            "level": record.levelname,
            "logger": record.name,
            "event": getattr(
                record,
                "event",
                record.getMessage(),
            ),
        }

        for field_name in ALLOWED_LOG_FIELDS:
            if field_name == "event":
                continue

            if hasattr(
                record,
                field_name,
            ):
                payload[field_name] = getattr(
                    record,
                    field_name,
                )

        return json.dumps(
            payload,
            separators=(",", ":"),
            ensure_ascii=False,
        )


def get_log_level() -> int:
    configured_level = os.getenv(
        "EDGE_LOG_LEVEL",
        DEFAULT_LOG_LEVEL,
    ).upper()

    numeric_level = getattr(
        logging,
        configured_level,
        None,
    )

    if not isinstance(
        numeric_level,
        int,
    ):
        raise RuntimeError(
            "EDGE_LOG_LEVEL must be a valid "
            "Python logging level."
        )

    return numeric_level


def configure_logging() -> logging.Logger:
    logger = logging.getLogger(
        LOGGER_NAME
    )

    logger.setLevel(
        get_log_level()
    )

    if not any(
        getattr(
            handler,
            "_privacycomply_handler",
            False,
        )
        for handler in logger.handlers
    ):
        handler = logging.StreamHandler()

        handler.setFormatter(
            JsonLogFormatter()
        )

        handler._privacycomply_handler = True

        logger.addHandler(
            handler
        )

    logger.propagate = False

    return logger