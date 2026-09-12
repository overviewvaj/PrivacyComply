from fastapi import HTTPException, Request
from fastapi.exceptions import RequestValidationError
from fastapi.responses import JSONResponse

from app.api_models import ApiErrorResponse
from app.logging_config import configure_logging


logger = configure_logging()


ERROR_CODE_BY_MESSAGE = {
    (
        "Unsupported file type. "
        "Only .xlsx and .csv files are supported."
    ):
        "UNSUPPORTED_FILE_TYPE",

    (
        "The uploaded file exceeds "
        "the configured maximum file size."
    ):
        "FILE_SIZE_LIMIT_EXCEEDED",

    (
        "The uploaded file content does not "
        "match the .xlsx file type."
    ):
        "XLSX_CONTENT_TYPE_MISMATCH",

    (
        "The XLSX file is invalid "
        "or corrupted."
    ):
        "INVALID_XLSX_FILE",

    (
        "The CSV file could not be decoded "
        "using UTF-8 encoding."
    ):
        "CSV_DECODING_ERROR",

    (
        "The CSV file is malformed "
        "and could not be parsed."
    ):
        "MALFORMED_CSV_FILE",

    (
        "The uploaded file exceeds "
        "the configured maximum "
        "number of columns."
    ):
        "COLUMN_LIMIT_EXCEEDED",

    (
        "The uploaded file exceeds "
        "the configured maximum "
        "number of data rows."
    ):
        "ROW_LIMIT_EXCEEDED",

    (
        "The uploaded file exceeds "
        "the configured maximum "
        "number of data cells."
    ):
        "CELL_LIMIT_EXCEEDED",

    (
        "The XLSX archive exceeds "
        "the configured maximum "
        "uncompressed size."
    ):
        "XLSX_UNCOMPRESSED_SIZE_LIMIT_EXCEEDED",

    (
        "The XLSX archive contains "
        "a suspiciously compressed "
        "entry."
    ):
        "XLSX_SUSPICIOUS_COMPRESSION",

    (
        "Edge Agent trust token "
        "is not configured."
    ):
        "EDGE_TRUST_NOT_CONFIGURED",

    (
        "Edge Agent trust token "
        "is required."
    ):
        "EDGE_TRUST_TOKEN_REQUIRED",

    (
        "Edge Agent trust token "
        "is invalid."
    ):
        "EDGE_TRUST_TOKEN_INVALID",
}


def get_error_code(
    status_code: int,
    message: str,
) -> str:
    mapped_error_code = (
        ERROR_CODE_BY_MESSAGE.get(
            message
        )
    )

    if mapped_error_code is not None:
        return mapped_error_code

    if status_code == 400:
        return "BAD_REQUEST"

    if status_code == 401:
        return "UNAUTHORIZED"

    if status_code == 413:
        return "PAYLOAD_TOO_LARGE"

    if status_code == 422:
        return "REQUEST_VALIDATION_ERROR"

    if status_code == 503:
        return "SERVICE_UNAVAILABLE"

    return "EDGE_AGENT_ERROR"


def build_error_response(
    *,
    error_code: str,
    message: str,
    status_code: int,
) -> JSONResponse:
    response_model = ApiErrorResponse(
        errorCode=error_code,
        message=message,
        statusCode=status_code,
    )

    return JSONResponse(
        status_code=status_code,
        content=response_model.model_dump(),
    )


async def http_exception_handler(
    request: Request,
    exception: HTTPException,
) -> JSONResponse:
    if isinstance(
        exception.detail,
        str,
    ):
        message = exception.detail
    else:
        message = (
            "The request could not "
            "be completed."
        )

    error_code = get_error_code(
        status_code=exception.status_code,
        message=message,
    )

    log_metadata = {
        "event":
            "api_request_failed",
        "path":
            request.url.path,
        "statusCode":
            exception.status_code,
        "errorCode":
            error_code,
    }

    uploaded_file_name = getattr(
        request.state,
        "uploaded_file_name",
        None,
    )

    if uploaded_file_name is not None:
        log_metadata[
            "fileName"
        ] = uploaded_file_name

    logger.warning(
        "api_request_failed",
        extra=log_metadata,
    )

    return build_error_response(
        error_code=error_code,
        message=message,
        status_code=exception.status_code,
    )


async def request_validation_exception_handler(
    request: Request,
    exception: RequestValidationError,
) -> JSONResponse:
    validation_errors = (
        exception.errors()
    )

    file_missing = any(
        (
            error.get("type") == "missing"
            and tuple(
                error.get(
                    "loc",
                    (),
                )
            )[-1:] == ("file",)
        )
        for error in validation_errors
    )

    if file_missing:
        error_code = "FILE_REQUIRED"

        logger.warning(
            "api_request_validation_failed",
            extra={
                "event":
                    "api_request_validation_failed",
                "path":
                    request.url.path,
                "statusCode":
                    422,
                "errorCode":
                    error_code,
            },
        )

        return build_error_response(
            error_code=error_code,
            message=(
                "A file upload is required."
            ),
            status_code=422,
        )

    error_code = (
        "REQUEST_VALIDATION_ERROR"
    )

    logger.warning(
        "api_request_validation_failed",
        extra={
            "event":
                "api_request_validation_failed",
            "path":
                request.url.path,
            "statusCode":
                422,
            "errorCode":
                error_code,
        },
    )

    return build_error_response(
        error_code=error_code,
        message=(
            "The request could not "
            "be validated."
        ),
        status_code=422,
    )