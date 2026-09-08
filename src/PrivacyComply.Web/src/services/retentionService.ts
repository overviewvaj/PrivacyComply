import type { RetentionCoverage } from '../types/RetentionCoverage'
import type { RetentionPolicy } from '../types/RetentionPolicy'
import { getJson } from './apiClient'

const retentionApiBasePath = '/api/retention'

export function getRetentionPolicies(): Promise<RetentionPolicy[]> {
    return getJson<RetentionPolicy[]>(
        `${retentionApiBasePath}/policies`,
        'Failed to load retention policies.',
    )
}

export function getRetentionCoverage(): Promise<RetentionCoverage[]> {
    return getJson<RetentionCoverage[]>(
        `${retentionApiBasePath}/coverage`,
        'Failed to load retention coverage.',
    )
}