from app.api_models import (
    ApiErrorResponse,
    ClassificationSummaryResponse,
    ColumnProfileResponse,
    FileInspectionResponse,
    FindingResponse,
    FindingSummaryResponse,
)


def test_classification_summary_model() -> None:
    model = (
        ClassificationSummaryResponse(
            totalColumns=2,
            classifiedColumns=2,
            unclassifiedColumns=0,
            classificationCoveragePercentage=100.0,
        )
    )

    assert model.totalColumns == 2

    assert (
        model.classificationCoveragePercentage
        == 100.0
    )


def test_finding_summary_model() -> None:
    model = FindingSummaryResponse(
        totalFindings=2,
        infoCount=1,
        warningCount=1,
        errorCount=0,
    )

    assert model.totalFindings == 2
    assert model.infoCount == 1
    assert model.warningCount == 1
    assert model.errorCount == 0


def test_nested_models_allow_existing_metadata() -> None:
    column_profile = (
        ColumnProfileResponse.model_validate(
            {
                "columnName": "email",
                "classificationStatus":
                    "CLASSIFIED",
                "classificationCode":
                    "EMAIL_ADDRESS",
                "classificationMethod":
                    "COLUMN_NAME_RULE",
                "matchPercentage": 100.0,
                "valueType": "TEXT",
                "nonEmptyCount": 2,
                "emptyCount": 0,
            }
        )
    )

    finding = (
        FindingResponse.model_validate(
            {
                "findingCode":
                    "MISSING_VALUES_DETECTED",
                "category":
                    "DATA_QUALITY",
                "severity":
                    "INFO",
                "columnName":
                    "email",
                "message":
                    "Missing values detected.",
            }
        )
    )

    assert (
        column_profile.columnName
        == "email"
    )

    assert (
        column_profile.classificationCode
        == "EMAIL_ADDRESS"
    )

    assert (
        finding.findingCode
        == "MISSING_VALUES_DETECTED"
    )


def test_file_inspection_response_model() -> None:
    response = (
        FileInspectionResponse.model_validate(
            {
                "fileName":
                    "customers.csv",
                "fileExtension":
                    ".csv",
                "contentType":
                    "text/csv",
                "fileSizeBytes":
                    100,
                "processingLocation":
                    "LOCAL_EDGE_AGENT",
                "sheetName":
                    None,
                "rowCount":
                    2,
                "columnCount":
                    2,
                "columns": [
                    "email",
                    "phone",
                ],
                "columnProfiles": [
                    {
                        "columnName":
                            "email",
                        "classificationStatus":
                            "CLASSIFIED",
                        "classificationCode":
                            "EMAIL_ADDRESS",
                        "classificationMethod":
                            "COLUMN_NAME_RULE",
                        "matchPercentage":
                            100.0,
                        "valueType":
                            "TEXT",
                        "nonEmptyCount":
                            2,
                        "emptyCount":
                            0,
                    },
                    {
                        "columnName":
                            "phone",
                        "classificationStatus":
                            "CLASSIFIED",
                        "classificationCode":
                            "PHONE_NUMBER",
                        "classificationMethod":
                            "COLUMN_NAME_RULE",
                        "matchPercentage":
                            100.0,
                        "valueType":
                            "TEXT",
                        "nonEmptyCount":
                            2,
                        "emptyCount":
                            0,
                    },
                ],
                "classificationSummary": {
                    "totalColumns":
                        2,
                    "classifiedColumns":
                        2,
                    "unclassifiedColumns":
                        0,
                    "classificationCoveragePercentage":
                        100.0,
                },
                "findingSummary": {
                    "totalFindings":
                        0,
                    "infoCount":
                        0,
                    "warningCount":
                        0,
                    "errorCount":
                        0,
                },
                "findings": [],
            }
        )
    )

    assert (
        response.fileName
        == "customers.csv"
    )

    assert (
        response.processingLocation
        == "LOCAL_EDGE_AGENT"
    )

    assert response.rowCount == 2
    assert response.columnCount == 2

    assert (
        response.classificationSummary
        .classifiedColumns
        == 2
    )

    assert (
        len(response.columnProfiles)
        == 2
    )


def test_empty_file_inspection_response_model() -> None:
    response = (
        FileInspectionResponse.model_validate(
            {
                "fileName":
                    "empty.csv",
                "fileExtension":
                    ".csv",
                "contentType":
                    "text/csv",
                "fileSizeBytes":
                    0,
                "processingLocation":
                    "LOCAL_EDGE_AGENT",
                "sheetName":
                    None,
                "rowCount":
                    0,
                "columnCount":
                    0,
                "columns":
                    [],
                "columnProfiles":
                    [],
                "classificationSummary": {
                    "totalColumns":
                        0,
                    "classifiedColumns":
                        0,
                    "unclassifiedColumns":
                        0,
                    "classificationCoveragePercentage":
                        0.0,
                },
                "findingSummary": {
                    "totalFindings":
                        0,
                    "infoCount":
                        0,
                    "warningCount":
                        0,
                    "errorCount":
                        0,
                },
                "findings":
                    [],
            }
        )
    )

    assert response.rowCount == 0
    assert response.columnCount == 0

    assert (
        response.columnProfiles
        == []
    )

    assert response.findings == []

def test_finding_model_allows_missing_category() -> None:
    finding = FindingResponse.model_validate(
        {
            "findingCode":
                "VALUE_PATTERN_CLASSIFICATION",
            "severity":
                "INFO",
            "columnName":
                "contact_value",
            "message":
                "Column classified using "
                "value-pattern analysis.",
        }
    )

    assert (
        finding.findingCode
        == "VALUE_PATTERN_CLASSIFICATION"
    )

    assert finding.category is None
    assert finding.severity == "INFO"

def test_api_error_response_model() -> None:
    error = ApiErrorResponse(
        errorCode="UNSUPPORTED_FILE_TYPE",
        message=(
            "Unsupported file type. "
            "Only .xlsx and .csv files "
            "are supported."
        ),
        statusCode=400,
    )

    assert (
        error.errorCode
        == "UNSUPPORTED_FILE_TYPE"
    )

    assert error.statusCode == 400

    assert (
        error.message
        == (
            "Unsupported file type. "
            "Only .xlsx and .csv files "
            "are supported."
        )
    )

def test_column_profile_allows_no_match_percentage() -> None:
    profile = ColumnProfileResponse.model_validate(
        {
            "columnName": "email",
            "classificationStatus":
                "CLASSIFIED",
            "classificationCode":
                "EMAIL_ADDRESS",
            "classificationMethod":
                "HEADER_RULE",
            "matchPercentage":
                None,
        }
    )

    assert (
        profile.classificationCode
        == "EMAIL_ADDRESS"
    )

    assert (
        profile.classificationMethod
        == "HEADER_RULE"
    )

    assert profile.matchPercentage is None