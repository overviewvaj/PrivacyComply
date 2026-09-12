from pydantic import BaseModel, ConfigDict


class ApiErrorResponse(
    BaseModel
):
    errorCode: str
    message: str
    statusCode: int


class ClassificationSummaryResponse(
    BaseModel
):
    totalColumns: int
    classifiedColumns: int
    unclassifiedColumns: int
    classificationCoveragePercentage: float


class FindingSummaryResponse(
    BaseModel
):
    totalFindings: int
    infoCount: int
    warningCount: int
    errorCount: int


class ColumnProfileResponse(
    BaseModel
):
    model_config = ConfigDict(
        extra="allow",
    )

    columnName: str
    classificationStatus: str
    classificationCode: str | None
    classificationMethod: str
    matchPercentage: float | None


class FindingResponse(
    BaseModel
):
    model_config = ConfigDict(
        extra="allow",
    )

    findingCode: str
    category: str | None = None
    severity: str


class FileInspectionResponse(
    BaseModel
):
    fileName: str
    fileExtension: str
    contentType: str | None
    fileSizeBytes: int
    processingLocation: str
    sheetName: str | None

    rowCount: int
    columnCount: int

    columns: list[str]

    columnProfiles: list[
        ColumnProfileResponse
    ]

    classificationSummary: (
        ClassificationSummaryResponse
    )

    findingSummary: FindingSummaryResponse

    findings: list[
        FindingResponse
    ]