import json
import logging

import pytest

from app.logging_config import (
    JsonLogFormatter,
    get_log_level,
)


def test_default_log_level(
    monkeypatch,
) -> None:
    monkeypatch.delenv(
        "EDGE_LOG_LEVEL",
        raising=False,
    )

    assert (
        get_log_level()
        == logging.INFO
    )


def test_configured_log_level(
    monkeypatch,
) -> None:
    monkeypatch.setenv(
        "EDGE_LOG_LEVEL",
        "DEBUG",
    )

    assert (
        get_log_level()
        == logging.DEBUG
    )


def test_invalid_log_level_rejected(
    monkeypatch,
) -> None:
    monkeypatch.setenv(
        "EDGE_LOG_LEVEL",
        "INVALID_LEVEL",
    )

    with pytest.raises(
        RuntimeError,
        match="valid Python logging level",
    ):
        get_log_level()


def test_json_log_formatter_outputs_safe_metadata() -> None:
    record = logging.LogRecord(
        name="PrivacyComply.EdgeAgent",
        level=logging.INFO,
        pathname="",
        lineno=0,
        msg="file_inspection_completed",
        args=(),
        exc_info=None,
    )

    record.event = (
        "file_inspection_completed"
    )

    record.fileName = (
        "customers.csv"
    )

    record.fileExtension = ".csv"
    record.fileSizeBytes = 100
    record.rowCount = 3
    record.columnCount = 2
    record.statusCode = 200

    formatter = JsonLogFormatter()

    payload = json.loads(
        formatter.format(record)
    )

    assert (
        payload["event"]
        == "file_inspection_completed"
    )

    assert (
        payload["fileName"]
        == "customers.csv"
    )

    assert payload["rowCount"] == 3
    assert payload["columnCount"] == 2
    assert payload["statusCode"] == 200


def test_json_log_formatter_excludes_raw_values() -> None:
    record = logging.LogRecord(
        name="PrivacyComply.EdgeAgent",
        level=logging.INFO,
        pathname="",
        lineno=0,
        msg="file_inspection_completed",
        args=(),
        exc_info=None,
    )

    record.event = (
        "file_inspection_completed"
    )

    record.fileName = "customers.csv"

    record.rawValue = (
        "alice@example.com"
    )

    record.fileContent = (
        "sensitive customer data"
    )

    record.dataRows = [
        ["alice@example.com"]
    ]

    formatter = JsonLogFormatter()

    formatted_log = (
        formatter.format(record)
    )

    assert (
        "alice@example.com"
        not in formatted_log
    )

    assert (
        "sensitive customer data"
        not in formatted_log
    )

    assert (
        "dataRows"
        not in formatted_log
    )

    assert (
        "rawValue"
        not in formatted_log
    )