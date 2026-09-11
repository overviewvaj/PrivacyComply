import type { RetentionCoverage } from '../types/RetentionCoverage'
import type { RetentionPolicy } from '../types/RetentionPolicy'
import { getJson } from './apiClient'

function getRetentionApiBasePath(
    organisationSlug: string,
): string {
    const normalizedOrganisationSlug =
        organisationSlug.trim()

    if (!normalizedOrganisationSlug) {
        throw new Error(
            'An organisation slug is required for retention requests.',
        )
    }

    return `/api/organisations/${encodeURIComponent(
        normalizedOrganisationSlug,
    )}/retention`
}

export function getRetentionPolicies(
    organisationSlug: string,
): Promise<RetentionPolicy[]> {
    const retentionApiBasePath =
        getRetentionApiBasePath(organisationSlug)

    return getJson<RetentionPolicy[]>(
        `${retentionApiBasePath}/policies`,
        'Failed to load retention policies.',
    )
}

export function getRetentionCoverage(
    organisationSlug: string,
): Promise<RetentionCoverage[]> {
    const retentionApiBasePath =
        getRetentionApiBasePath(organisationSlug)

    return getJson<RetentionCoverage[]>(
        `${retentionApiBasePath}/coverage`,
        'Failed to load retention coverage.',
    )
}