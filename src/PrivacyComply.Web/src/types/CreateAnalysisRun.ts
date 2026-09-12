export interface CreateAnalysisRunRequest {
    sourceTypeCode: 'EXCEL' | 'CSV'
    sourceName: string
}

export interface CreateAnalysisRunResult {
    analysisRunId: string
    analysisRunCode: string
    runStatusCode: string
    requestedDateTime: string
}

export interface CompleteAnalysisRunRequest {
    sourceObjectName: string | null
    totalRecordsAnalysed: number
    totalFieldsDiscovered: number
    totalUnclassifiedFields: number
    classificationCoveragePercentage: number
}