export type RetentionPolicy = {
    retentionPolicyId: string
    retentionPolicyCode: string
    retentionPolicyName: string
    description: string | null
    retentionPeriodValue: number
    retentionPeriodUnitCode: string
    retentionTriggerCode: string
    expiryActionCode: string
    reviewRequired: boolean
    statusCode: string
    effectiveFromDate: string
    effectiveToDate: string | null
    policyVersion: number
}