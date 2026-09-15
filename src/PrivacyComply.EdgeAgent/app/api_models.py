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
    totalPersonalDataColumns: int = 0
    totalContextDependentColumns: int = 0
    totalRegulatedIdentifierColumns: int = 0


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
    privacyCategory: str | None = None
    isPersonalData: bool = False
    isRegulatedIdentifier: bool = False


class FindingResponse(
    BaseModel
):
    model_config = ConfigDict(
        extra="allow",
    )

    findingCode: str
    category: str | None = None
    severity: str


class EvidenceItemResponse(
    BaseModel
):
    model_config = ConfigDict(
        extra="allow",
    )

    evidenceReference: str
    evidenceType: str
    sourceReference: str
    fieldName: str | None = None
    classificationCode: str | None = None
    ruleVersion: str
    agentVersion: str
    evidenceHash: str
    hashAlgorithm: str
    metadataJson: str


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

    evidence: list[
        EvidenceItemResponse
    ] = []
