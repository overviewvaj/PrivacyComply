import asyncio
from io import BytesIO

import pytest
from fastapi import HTTPException, UploadFile
from openpyxl import Workbook
from starlette.datastructures import Headers

from app.file_inspector import (
    build_empty_result,
    inspect_csv,
    inspect_uploaded_file,
    inspect_xlsx,
)


def create_upload_file(
    filename: str,
    content: bytes,
    content_type: str,
) -> UploadFile:
    return UploadFile(
        file=BytesIO(content),
        filename=filename,
        headers=Headers(
            {
                "content-type": content_type,
            }
        ),
    )


def test_csv_inspection() -> None:
    content = (
        "contact_value\n"
        "alice@example.com\n"
        "bob@example.org\n"
        "charlie@example.net\n"
    ).encode("utf-8")

    file = create_upload_file(
        filename="contacts.csv",
        content=content,
        content_type="text/csv",
    )

    result = inspect_csv(
        file=file,
        file_name="contacts.csv",
        file_extension=".csv",
        file_content=content,
    )

    assert result["fileName"] == "contacts.csv"
    assert result["fileExtension"] == ".csv"
    assert result["contentType"] == "text/csv"
    assert result["processingLocation"] == "LOCAL_EDGE_AGENT"
    assert result["sheetName"] is None
    assert result["rowCount"] == 3
    assert result["columnCount"] == 1

    assert result["columns"] == [
        "contact_value",
    ]

    profile = result["columnProfiles"][0]

    assert profile["classificationStatus"] == "CLASSIFIED"
    assert profile["classificationCode"] == "EMAIL_ADDRESS"
    assert profile["classificationMethod"] == "VALUE_PATTERN_RULE"
    assert profile["matchPercentage"] == 100.0


def test_csv_header_whitespace_is_trimmed() -> None:
    content = (
        "  email_address  ,  notes  \n"
        "alice@example.com,alpha\n"
    ).encode("utf-8")

    file = create_upload_file(
        filename="contacts.csv",
        content=content,
        content_type="text/csv",
    )

    result = inspect_csv(
        file=file,
        file_name="contacts.csv",
        file_extension=".csv",
        file_content=content,
    )

    assert result["columns"] == [
        "email_address",
        "notes",
    ]


def test_empty_csv_returns_empty_result() -> None:
    content = b""

    file = create_upload_file(
        filename="empty.csv",
        content=content,
        content_type="text/csv",
    )

    result = inspect_csv(
        file=file,
        file_name="empty.csv",
        file_extension=".csv",
        file_content=content,
    )

    assert result["rowCount"] == 0
    assert result["columnCount"] == 0
    assert result["columns"] == []
    assert result["columnProfiles"] == []
    assert result["findings"] == []


def test_invalid_utf8_csv_is_rejected() -> None:
    content = b"\xff\xfe\xfa"

    file = create_upload_file(
        filename="invalid.csv",
        content=content,
        content_type="text/csv",
    )

    with pytest.raises(
        HTTPException
    ) as exception_info:
        inspect_csv(
            file=file,
            file_name="invalid.csv",
            file_extension=".csv",
            file_content=content,
        )

    assert exception_info.value.status_code == 400

    assert (
        exception_info.value.detail
        == (
            "The CSV file could not be decoded "
            "using UTF-8 encoding."
        )
    )


def test_xlsx_inspection() -> None:
    workbook = Workbook()

    worksheet = workbook.active
    worksheet.title = "Customer Data"

    worksheet.append(
        [
            "PAN Number",
            "Email Address",
        ]
    )

    worksheet.append(
        [
            "ABCDE1234F",
            "alice@example.com",
        ]
    )

    worksheet.append(
        [
            "PQRSX5678K",
            "bob@example.org",
        ]
    )

    buffer = BytesIO()
    workbook.save(buffer)

    content = buffer.getvalue()

    file = create_upload_file(
        filename="customers.xlsx",
        content=content,
        content_type=(
            "application/vnd.openxmlformats-"
            "officedocument.spreadsheetml.sheet"
        ),
    )

    result = inspect_xlsx(
        file=file,
        file_name="customers.xlsx",
        file_extension=".xlsx",
        file_content=content,
    )

    assert result["fileName"] == "customers.xlsx"
    assert result["sheetName"] == "Customer Data"
    assert result["rowCount"] == 2
    assert result["columnCount"] == 2

    assert result["columns"] == [
        "PAN Number",
        "Email Address",
    ]

    assert (
        result["columnProfiles"][0]["classificationCode"]
        == "INDIA_PAN"
    )

    assert (
        result["columnProfiles"][1]["classificationCode"]
        == "EMAIL_ADDRESS"
    )


def test_unsupported_file_extension_is_rejected() -> None:
    file = create_upload_file(
        filename="customers.txt",
        content=b"test",
        content_type="text/plain",
    )

    with pytest.raises(
        HTTPException
    ) as exception_info:
        asyncio.run(
            inspect_uploaded_file(file)
        )

    assert exception_info.value.status_code == 400

    assert (
        exception_info.value.detail
        == (
            "Unsupported file type. "
            "Only .xlsx and .csv files are supported."
        )
    )


def test_uploaded_csv_uses_full_pipeline() -> None:
    content = (
        "identity_value\n"
        "27ABCDE1234F1Z0\n"
        "29PQRSX5678K1ZU\n"
        "07AAAAA1111A1ZY\n"
    ).encode("utf-8")

    file = create_upload_file(
        filename="gstin.csv",
        content=content,
        content_type="text/csv",
    )

    result = asyncio.run(
        inspect_uploaded_file(file)
    )

    assert result["rowCount"] == 3
    assert result["columnCount"] == 1

    profile = result["columnProfiles"][0]

    assert profile["classificationStatus"] == "CLASSIFIED"
    assert profile["classificationCode"] == "INDIA_GSTIN"
    assert profile["classificationMethod"] == "VALUE_PATTERN_RULE"
    assert profile["matchPercentage"] == 100.0

    assert (
        result["classificationSummary"][
            "classificationCoveragePercentage"
        ]
        == 100.0
    )

    assert result["findingSummary"]["totalFindings"] == 1


def test_build_empty_result() -> None:
    file = create_upload_file(
        filename="empty.csv",
        content=b"",
        content_type="text/csv",
    )

    result = build_empty_result(
        file=file,
        file_name="empty.csv",
        file_extension=".csv",
        file_content=b"",
        sheet_name=None,
    )

    assert result["processingLocation"] == "LOCAL_EDGE_AGENT"
    assert result["rowCount"] == 0
    assert result["columnCount"] == 0

    assert result["classificationSummary"] == {
        "totalColumns": 0,
        "classifiedColumns": 0,
        "unclassifiedColumns": 0,
        "classificationCoveragePercentage": 0.0,
    }

    assert result["findingSummary"] == {
        "totalFindings": 0,
        "infoCount": 0,
        "warningCount": 0,
        "errorCount": 0,
    }