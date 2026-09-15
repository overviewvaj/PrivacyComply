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

export interface DiscoveredFieldPayload {
    sourceObjectName?: string | null
    fieldName: string
    ordinalPosition: number
    inferredDataType: string
    classificationStatus: string
    classificationCode?: string | null
    classificationMethod: string
    matchPercentage?: number | null
    privacyCategory: string
    isPersonalData: boolean
    isRegulatedIdentifier: boolean
    nonEmptyCount: number
    emptyCount: number
}

export interface FindingPayload {
    findingCode: string
    findingCategory: string
    severity: string
    fieldName?: string | null
    ruleReference?: string | null
    message?: string | null
    safeMetadataJson?: string | null
}

export interface EvidenceRecordPayload {
    evidenceReference: string
    evidenceTypeCode: string
    sourceTypeCode: string
    sourceReference?: string | null
    fieldName?: string | null
    classificationCode?: string | null
    ruleVersion?: string | null
    agentVersion?: string | null
    evidenceHash: string
    hashAlgorithmCode: string
    metadataJson?: string | null
}

export interface CompleteAnalysisRunRequest {
    sourceObjectName: string | null
    totalRecordsAnalysed: number
    totalFieldsDiscovered: number
    totalPersonalDataFields: number
    totalUnclassifiedFields: number
    classificationCoveragePercentage: number
    discoveredFields?: DiscoveredFieldPayload[]
    findings?: FindingPayload[]
    evidence?: EvidenceRecordPayload[]
}

export interface DiscoveredFieldDto {
    discoveredFieldId: string
    analysisRunId: string
    sourceObjectName: string | null
    fieldName: string
    ordinalPosition: number
    inferredDataType: string
    classificationStatus: string
    classificationCode: string | null
    classificationMethod: string
    matchPercentage: number | null
    privacyCategory: string
    isPersonalData: boolean
    isRegulatedIdentifier: boolean
    nonEmptyCount: number
    emptyCount: number
    findingCount: number
    createdDateTime: string
}

export interface FindingDto {
    findingId: string
    analysisRunId: string
    discoveredFieldId: string | null
    findingCode: string
    findingCategory: string
    severity: string
    fieldName: string | null
    ruleReference: string | null
    findingStatus: string
    detectedDateTime: string
    resolvedDateTime: string | null
    message: string | null
    safeMetadataJson: string | null
    createdDateTime: string
}


export interface EvidenceDto {
    evidenceId: string
    organisationId: string
    analysisRunId: string
    discoveredFieldId: string | null
    evidenceReference: string
    evidenceType: string
    sourceType: string
    sourceReference: string | null
    ruleVersion: string | null
    agentVersion: string | null
    evidenceHash: string
    hashAlgorithmCode: string
    generatedDateTime: string
    capturedBy: string | null
    isVerified: boolean
    verificationStatusCode: string
    metadataJson: string | null
    fieldName: string | null
    sourceObjectName: string | null
    classificationCode: string | null
    privacyCategory: string | null
    isPersonalData: boolean | null
    isRegulatedIdentifier: boolean | null
}

export interface RuleEvaluationDto {
    ruleEvaluationId: string
    organisationId: string
    analysisRunId: string
    analysisRunCode: string | null
    frameworkCode: string
    frameworkVersion: string
    ruleCode: string
    ruleVersion: string
    ruleName: string
    regulatoryReference: string | null
    evaluationOutcome: 'PASS' | 'FAIL' | 'REVIEW_REQUIRED' | 'NOT_APPLICABLE'
    severity: string
    summaryMessage: string
    evaluatedFieldsCount: number
    flaggedFieldsCount: number
    flaggedFieldNamesJson: string | null
    evaluatedDateTime: string
    createdDateTime: string
    createdBy: string
}
