export interface AnalysisRun {
    analysisRunId: string
    analysisRunCode: string
    runStatusCode: string
    sourceTypeCode: string
    sourceName: string | null
    sourceObjectName: string | null
    requestedDateTime: string
    startedDateTime: string | null
    completedDateTime: string | null
    failedDateTime: string | null
    cancelledDateTime: string | null
    totalRecordsAnalysed: number | null
    totalFieldsDiscovered: number | null
    totalPersonalDataFields: number | null
    totalSensitiveDataFields: number | null
    totalUnclassifiedFields: number | null
    classificationCoveragePercentage: number | null
}