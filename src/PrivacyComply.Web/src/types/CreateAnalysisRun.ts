export interface CreateAnalysisRunRequest {
    sourceTypeCode: 'EXCEL' | 'CSV'
    sourceName: string
    sourceObjectName: string | null
}

export interface CreateAnalysisRunResult {
    analysisRunId: string
    analysisRunCode: string
    runStatusCode: string
    requestedDateTime: string
}