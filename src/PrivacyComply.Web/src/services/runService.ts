import type { AnalysisRun } from '../types/AnalysisRun'

import type {
    CreateAnalysisRunRequest,
    CreateAnalysisRunResult,
} from '../types/CreateAnalysisRun'

import {
    getJson,
    postJson,
} from './apiClient'

interface CancelAnalysisRunResult {
    analysisRunId: string
    analysisRunCode: string
    runStatusCode: string
}

function getRunApiBasePath(
    organisationSlug: string,
): string {
    const normalizedOrganisationSlug =
        organisationSlug.trim()

    if (!normalizedOrganisationSlug) {
        throw new Error(
            'An organisation slug is required for run requests.',
        )
    }

    return `/api/organisations/${encodeURIComponent(
        normalizedOrganisationSlug,
    )}/runs`
}

export function getAnalysisRuns(
    organisationSlug: string,
): Promise<AnalysisRun[]> {
    const runApiBasePath =
        getRunApiBasePath(organisationSlug)

    return getJson<AnalysisRun[]>(
        runApiBasePath,
        'Failed to load analysis runs.',
    )
}

export function createAnalysisRun(
    organisationSlug: string,
    request: CreateAnalysisRunRequest,
): Promise<CreateAnalysisRunResult> {
    const runApiBasePath =
        getRunApiBasePath(organisationSlug)

    return postJson<
        CreateAnalysisRunRequest,
        CreateAnalysisRunResult
    >(
        runApiBasePath,
        request,
        'Failed to create analysis run.',
    )
}

export function cancelAnalysisRun(
    organisationSlug: string,
    analysisRunId: string,
): Promise<CancelAnalysisRunResult> {
    const runApiBasePath =
        getRunApiBasePath(organisationSlug)

    const normalizedAnalysisRunId =
        analysisRunId.trim()

    if (!normalizedAnalysisRunId) {
        throw new Error(
            'An analysis run identifier is required.',
        )
    }

    return postJson<
        Record<string, never>,
        CancelAnalysisRunResult
    >(
        `${runApiBasePath}/${encodeURIComponent(
            normalizedAnalysisRunId,
        )}/cancel`,
        {},
        'Failed to cancel analysis run.',
    )
}