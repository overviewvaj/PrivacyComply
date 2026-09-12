import type { AnalysisRun } from '../types/AnalysisRun'

import type {
    CompleteAnalysisRunRequest,
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

function normalizeAnalysisRunId(
    analysisRunId: string,
): string {
    const normalizedAnalysisRunId =
        analysisRunId.trim()

    if (!normalizedAnalysisRunId) {
        throw new Error(
            'An analysis run identifier is required.',
        )
    }

    return normalizedAnalysisRunId
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

export function startAnalysisRun(
    organisationSlug: string,
    analysisRunId: string,
): Promise<void> {
    const runApiBasePath =
        getRunApiBasePath(organisationSlug)

    const normalizedAnalysisRunId =
        normalizeAnalysisRunId(
            analysisRunId,
        )

    return postJson<
        Record<string, never>,
        void
    >(
        `${runApiBasePath}/${encodeURIComponent(
            normalizedAnalysisRunId,
        )}/start`,
        {},
        'Failed to start analysis run.',
    )
}

export function completeAnalysisRun(
    organisationSlug: string,
    analysisRunId: string,
    request: CompleteAnalysisRunRequest,
): Promise<void> {
    const runApiBasePath =
        getRunApiBasePath(organisationSlug)

    const normalizedAnalysisRunId =
        normalizeAnalysisRunId(
            analysisRunId,
        )

    return postJson<
        CompleteAnalysisRunRequest,
        void
    >(
        `${runApiBasePath}/${encodeURIComponent(
            normalizedAnalysisRunId,
        )}/complete`,
        request,
        'Failed to complete analysis run.',
    )
}

export function failAnalysisRun(
    organisationSlug: string,
    analysisRunId: string,
    failureCode?: string,
): Promise<void> {
    const runApiBasePath =
        getRunApiBasePath(organisationSlug)

    const normalizedAnalysisRunId =
        normalizeAnalysisRunId(
            analysisRunId,
        )

    const normalizedFailureCode =
        failureCode?.trim()

    const queryString =
        normalizedFailureCode
            ? `?failureCode=${encodeURIComponent(
                normalizedFailureCode,
            )}`
            : ''

    return postJson<
        Record<string, never>,
        void
    >(
        `${runApiBasePath}/${encodeURIComponent(
            normalizedAnalysisRunId,
        )}/fail${queryString}`,
        {},
        'Failed to mark analysis run as failed.',
    )
}

export function cancelAnalysisRun(
    organisationSlug: string,
    analysisRunId: string,
): Promise<CancelAnalysisRunResult> {
    const runApiBasePath =
        getRunApiBasePath(organisationSlug)

    const normalizedAnalysisRunId =
        normalizeAnalysisRunId(
            analysisRunId,
        )

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