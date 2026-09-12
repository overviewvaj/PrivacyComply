export interface EdgeApiErrorResponse {
    errorCode: string
    message: string
    statusCode: number
}

export interface EdgeColumnProfile {
    columnName: string
    classificationStatus: string
    classificationCode: string | null
    classificationMethod: string
    matchPercentage: number | null
}

export interface EdgeClassificationSummary {
    totalColumns: number
    classifiedColumns: number
    unclassifiedColumns: number
    classificationCoveragePercentage: number
}

export interface EdgeFindingSummary {
    totalFindings: number
    infoCount: number
    warningCount: number
    errorCount: number
}

export interface EdgeFinding {
    findingCode: string
    category?: string | null
    severity: string

    [key: string]: unknown
}

export interface EdgeInspectionResult {
    fileName: string
    fileExtension: string
    contentType: string | null
    fileSizeBytes: number
    processingLocation: string
    sheetName: string | null

    rowCount: number
    columnCount: number

    columns: string[]

    columnProfiles: EdgeColumnProfile[]

    classificationSummary:
    EdgeClassificationSummary

    findingSummary:
    EdgeFindingSummary

    findings: EdgeFinding[]
}