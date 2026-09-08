export type RetentionCoverage = {
    processingActivityId: string
    processingActivityCode: string
    processingActivityName: string
    retentionPolicyId: string | null
    retentionPolicyCode: string | null
    retentionPolicyName: string | null
    hasRetentionPolicy: boolean
}