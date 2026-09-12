from fastapi import (
    Depends,
    FastAPI,
    File,
    HTTPException,
    Request,
    UploadFile,
)

from fastapi.exceptions import (
    RequestValidationError,
)

from app.api_errors import (
    http_exception_handler,
    request_validation_exception_handler,
)

from app.api_models import (
    ApiErrorResponse,
    FileInspectionResponse,
)

from app.cors_config import (
    configure_cors,
)

from app.file_inspector import (
    inspect_uploaded_file,
)

from app.logging_config import (
    configure_logging,
)

from app.trust_config import (
    verify_edge_trust,
)


logger = configure_logging()


app = FastAPI(
    title="PrivacyComply Edge Agent",
    version="0.1.0",
)


configure_cors(
    app
)


app.add_exception_handler(
    HTTPException,
    http_exception_handler,
)

app.add_exception_handler(
    RequestValidationError,
    request_validation_exception_handler,
)


@app.get("/health")
async def health() -> dict[str, str]:
    return {
        "status": "healthy",
        "service":
            "PrivacyComply.EdgeAgent",
    }


@app.post(
    "/analysis/files/inspect",
    response_model=FileInspectionResponse,
    responses={
        400: {
            "model": ApiErrorResponse,
        },
        401: {
            "model": ApiErrorResponse,
        },
        413: {
            "model": ApiErrorResponse,
        },
        422: {
            "model": ApiErrorResponse,
        },
        503: {
            "model": ApiErrorResponse,
        },
    },
)
async def inspect_file(
    request: Request,
    file: UploadFile = File(...),
    _: None = Depends(
        verify_edge_trust
    ),
) -> FileInspectionResponse:
    file_name = file.filename or ""

    request.state.uploaded_file_name = (
        file_name
    )

    logger.info(
        "file_inspection_started",
        extra={
            "event":
                "file_inspection_started",
            "path":
                request.url.path,
            "fileName":
                file_name,
            "contentType":
                file.content_type,
        },
    )

    inspection_result = (
        await inspect_uploaded_file(
            file,
        )
    )

    classification_summary = (
        inspection_result[
            "classificationSummary"
        ]
    )

    finding_summary = (
        inspection_result[
            "findingSummary"
        ]
    )

    logger.info(
        "file_inspection_completed",
        extra={
            "event":
                "file_inspection_completed",
            "path":
                request.url.path,
            "fileName":
                inspection_result[
                    "fileName"
                ],
            "fileExtension":
                inspection_result[
                    "fileExtension"
                ],
            "contentType":
                inspection_result[
                    "contentType"
                ],
            "fileSizeBytes":
                inspection_result[
                    "fileSizeBytes"
                ],
            "rowCount":
                inspection_result[
                    "rowCount"
                ],
            "columnCount":
                inspection_result[
                    "columnCount"
                ],
            "classifiedColumns":
                classification_summary[
                    "classifiedColumns"
                ],
            "totalFindings":
                finding_summary[
                    "totalFindings"
                ],
            "statusCode":
                200,
        },
    )

    return FileInspectionResponse.model_validate(
        inspection_result
    )