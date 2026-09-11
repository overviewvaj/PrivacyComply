import csv
from io import BytesIO, StringIO
from pathlib import Path
from typing import Any

from fastapi import HTTPException, UploadFile
from openpyxl import load_workbook

from app.file_profiler import (
    build_classification_summary,
    build_column_profiles,
)

from app.finding_builder import (
    build_finding_summary,
    build_findings,
)


SUPPORTED_EXTENSIONS = {
    ".xlsx",
    ".csv",
}


async def inspect_uploaded_file(
    file: UploadFile,
) -> dict[str, Any]:
    file_name = file.filename or ""

    file_extension = (
        Path(file_name)
        .suffix
        .lower()
    )

    if file_extension not in SUPPORTED_EXTENSIONS:
        raise HTTPException(
            status_code=400,
            detail=(
                "Unsupported file type. "
                "Only .xlsx and .csv files are supported."
            ),
        )

    file_content = await file.read()

    if file_extension == ".xlsx":
        return inspect_xlsx(
            file=file,
            file_name=file_name,
            file_extension=file_extension,
            file_content=file_content,
        )

    return inspect_csv(
        file=file,
        file_name=file_name,
        file_extension=file_extension,
        file_content=file_content,
    )


def inspect_xlsx(
    file: UploadFile,
    file_name: str,
    file_extension: str,
    file_content: bytes,
) -> dict[str, Any]:
    workbook = load_workbook(
        filename=BytesIO(file_content),
        read_only=True,
        data_only=True,
    )

    worksheet = workbook.active

    rows = [
        list(row)
        for row in worksheet.iter_rows(
            values_only=True,
        )
    ]

    if not rows:
        return build_empty_result(
            file=file,
            file_name=file_name,
            file_extension=file_extension,
            file_content=file_content,
            sheet_name=worksheet.title,
        )

    columns = [
        str(value).strip()
        if value is not None
        else ""
        for value in rows[0]
    ]

    data_rows = rows[1:]

    return build_result(
        file=file,
        file_name=file_name,
        file_extension=file_extension,
        file_content=file_content,
        sheet_name=worksheet.title,
        columns=columns,
        data_rows=data_rows,
    )


def inspect_csv(
    file: UploadFile,
    file_name: str,
    file_extension: str,
    file_content: bytes,
) -> dict[str, Any]:
    try:
        decoded_content = file_content.decode(
            "utf-8-sig",
        )
    except UnicodeDecodeError as exception:
        raise HTTPException(
            status_code=400,
            detail=(
                "The CSV file could not be decoded "
                "using UTF-8 encoding."
            ),
        ) from exception

    csv_reader = csv.reader(
        StringIO(decoded_content)
    )

    rows = [
        list(row)
        for row in csv_reader
    ]

    if not rows:
        return build_empty_result(
            file=file,
            file_name=file_name,
            file_extension=file_extension,
            file_content=file_content,
            sheet_name=None,
        )

    columns = [
        value.strip()
        for value in rows[0]
    ]

    data_rows = rows[1:]

    return build_result(
        file=file,
        file_name=file_name,
        file_extension=file_extension,
        file_content=file_content,
        sheet_name=None,
        columns=columns,
        data_rows=data_rows,
    )


def build_result(
    file: UploadFile,
    file_name: str,
    file_extension: str,
    file_content: bytes,
    sheet_name: str | None,
    columns: list[str],
    data_rows: list[list[Any]],
) -> dict[str, Any]:
    column_profiles = build_column_profiles(
        columns,
        data_rows,
    )

    classification_summary = (
        build_classification_summary(
            column_profiles,
        )
    )

    findings = build_findings(
        column_profiles,
    )

    finding_summary = build_finding_summary(
        findings,
    )

    return {
        "fileName": file_name,
        "fileExtension": file_extension,
        "contentType": file.content_type,
        "fileSizeBytes": len(file_content),
        "processingLocation":
            "LOCAL_EDGE_AGENT",
        "sheetName": sheet_name,
        "rowCount": len(data_rows),
        "columnCount": len(columns),
        "columns": columns,
        "columnProfiles": column_profiles,
        "classificationSummary":
            classification_summary,
        "findingSummary":
            finding_summary,
        "findings": findings,
    }


def build_empty_result(
    file: UploadFile,
    file_name: str,
    file_extension: str,
    file_content: bytes,
    sheet_name: str | None,
) -> dict[str, Any]:
    return {
        "fileName": file_name,
        "fileExtension": file_extension,
        "contentType": file.content_type,
        "fileSizeBytes": len(file_content),
        "processingLocation":
            "LOCAL_EDGE_AGENT",
        "sheetName": sheet_name,
        "rowCount": 0,
        "columnCount": 0,
        "columns": [],
        "columnProfiles": [],
        "classificationSummary": {
            "totalColumns": 0,
            "classifiedColumns": 0,
            "unclassifiedColumns": 0,
            "classificationCoveragePercentage": 0.0,
        },
        "findingSummary": {
            "totalFindings": 0,
            "infoCount": 0,
            "warningCount": 0,
            "errorCount": 0,
        },
        "findings": [],
    }