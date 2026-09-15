import {
    useCallback,
    useEffect,
    useMemo,
    useState,
} from 'react'

import {
    useNavigate,
    useParams,
} from 'react-router-dom'

import {
    cancelAnalysisRun,
    getAnalysisRuns,
} from '../../services/runService'

import type {
    AnalysisRun,
} from '../../types/AnalysisRun'

import './RunsPage.css'

type StatusFilter = 'ALL' | 'COMPLETED' | 'QUEUED' | 'FAILED'

function formatLocalDateTime(
    dateTime: string,
): string {
    const normalizedUtcDateTime =
        dateTime.endsWith('Z') ||
            /[+-]\d{2}:\d{2}$/.test(
                dateTime,
            )
            ? dateTime
            : `${dateTime}Z`

    const parsedDate =
        new Date(normalizedUtcDateTime)

    if (Number.isNaN(parsedDate.getTime())) {
        return dateTime
    }

    return new Intl.DateTimeFormat(
        undefined,
        {
            dateStyle: 'medium',
            timeStyle: 'short',
        },
    ).format(parsedDate)
}

function RunsPage() {
    const { organisationSlug } = useParams<{
        organisationSlug: string
    }>()

    const navigate = useNavigate()

    const [runs, setRuns] =
        useState<AnalysisRun[]>([])

    const [isLoading, setIsLoading] =
        useState(true)

    const [errorMessage, setErrorMessage] =
        useState<string | null>(null)

    const [
        cancellingRunId,
        setCancellingRunId,
    ] = useState<string | null>(null)

    const [statusFilter, setStatusFilter] =
        useState<StatusFilter>('ALL')

    const [currentPage, setCurrentPage] =
        useState(1)

    const pageSize = 10

    const loadRuns = useCallback(
        async () => {
            if (!organisationSlug) {
                setErrorMessage(
                    'A valid organisation context is required.',
                )

                setIsLoading(false)

                return
            }

            try {
                setIsLoading(true)
                setErrorMessage(null)

                const loadedRuns =
                    await getAnalysisRuns(
                        organisationSlug,
                    )

                setRuns(loadedRuns)
            } catch (error) {
                setErrorMessage(
                    error instanceof Error
                        ? error.message
                        : 'Failed to load analysis runs.',
                )
            } finally {
                setIsLoading(false)
            }
        },
        [organisationSlug],
    )

    useEffect(() => {
        void loadRuns()
    }, [loadRuns])

    const handleCancelRun = async (
        analysisRunId: string,
    ) => {
        if (
            !organisationSlug ||
            cancellingRunId !== null
        ) {
            return
        }

        try {
            setCancellingRunId(
                analysisRunId,
            )

            await cancelAnalysisRun(
                organisationSlug,
                analysisRunId,
            )

            await loadRuns()
        } catch (error) {
            setErrorMessage(
                error instanceof Error
                    ? error.message
                    : 'Failed to cancel the analysis run.',
            )
        } finally {
            setCancellingRunId(null)
        }
    }

    // Status counts
    const countAll = runs.length
    const countCompleted = useMemo(
        () => runs.filter(r => r.runStatusCode === 'COMPLETED').length,
        [runs]
    )
    const countQueued = useMemo(
        () => runs.filter(r => r.runStatusCode === 'QUEUED').length,
        [runs]
    )
    const countFailed = useMemo(
        () => runs.filter(r => r.runStatusCode === 'FAILED').length,
        [runs]
    )

    // Filtered runs
    const filteredRuns = useMemo(() => {
        if (statusFilter === 'ALL') {
            return runs
        }
        return runs.filter(r => r.runStatusCode === statusFilter)
    }, [runs, statusFilter])

    // Pagination
    const totalPages = Math.max(1, Math.ceil(filteredRuns.length / pageSize))
    const startIndex = (currentPage - 1) * pageSize
    const paginatedRuns = useMemo(() => {
        return filteredRuns.slice(startIndex, startIndex + pageSize)
    }, [filteredRuns, startIndex, pageSize])

    const handleFilterChange = (newFilter: StatusFilter) => {
        setStatusFilter(newFilter)
        setCurrentPage(1)
    }

    const getStatusClass = (status: string): string => {
        switch (status) {
            case 'COMPLETED':
                return 'runs-page__run-status--completed'
            case 'RUNNING':
                return 'runs-page__run-status--running'
            case 'QUEUED':
                return 'runs-page__run-status--queued'
            case 'FAILED':
                return 'runs-page__run-status--failed'
            case 'CANCELLED':
                return 'runs-page__run-status--cancelled'
            default:
                return ''
        }
    }

    return (
        <section className="runs-page">
            <header className="runs-page__header">
                <div>
                    <p className="runs-page__eyebrow">
                        Discovery & Verification
                    </p>

                    <h1 className="runs-page__title">
                        Analysis Runs
                    </h1>

                    <p className="runs-page__description">
                        Manage central analysis runs for
                        privacy discovery and local Edge
                        Agent inspections.
                    </p>
                </div>

                <button
                    type="button"
                    className="runs-page__new-run-button"
                    onClick={() => navigate('new')}
                >
                    + New Run
                </button>
            </header>

            <section className="runs-page__panel">
                <div className="runs-page__panel-header">
                    <h2 className="runs-page__panel-title">
                        Analysis run history
                    </h2>

                    {/* Status Filter Tabs */}
                    <div className="runs-page__filters">
                        <button
                            type="button"
                            className={`runs-page__filter-btn ${statusFilter === 'ALL' ? 'runs-page__filter-btn--active' : ''}`}
                            onClick={() => handleFilterChange('ALL')}
                        >
                            All
                            <span className="runs-page__filter-badge">{countAll}</span>
                        </button>

                        <button
                            type="button"
                            className={`runs-page__filter-btn ${statusFilter === 'COMPLETED' ? 'runs-page__filter-btn--active' : ''}`}
                            onClick={() => handleFilterChange('COMPLETED')}
                        >
                            Completed
                            <span className="runs-page__filter-badge runs-page__filter-badge--completed">{countCompleted}</span>
                        </button>

                        <button
                            type="button"
                            className={`runs-page__filter-btn ${statusFilter === 'QUEUED' ? 'runs-page__filter-btn--active' : ''}`}
                            onClick={() => handleFilterChange('QUEUED')}
                        >
                            Queued
                            <span className="runs-page__filter-badge runs-page__filter-badge--queued">{countQueued}</span>
                        </button>

                        <button
                            type="button"
                            className={`runs-page__filter-btn ${statusFilter === 'FAILED' ? 'runs-page__filter-btn--active' : ''}`}
                            onClick={() => handleFilterChange('FAILED')}
                        >
                            Failed
                            <span className="runs-page__filter-badge runs-page__filter-badge--failed">{countFailed}</span>
                        </button>
                    </div>
                </div>

                {isLoading && (
                    <p className="runs-page__message">
                        Loading analysis runs...
                    </p>
                )}

                {errorMessage && (
                    <p className="runs-page__error">
                        {errorMessage}
                    </p>
                )}

                {!isLoading &&
                    !errorMessage &&
                    filteredRuns.length === 0 && (
                        <p className="runs-page__message">
                            {runs.length === 0
                                ? 'No analysis runs are currently available.'
                                : `No analysis runs with status "${statusFilter}" were found.`}
                        </p>
                    )}

                {!isLoading &&
                    filteredRuns.length > 0 && (
                        <>
                            <div className="runs-page__list">
                                {paginatedRuns.map((run) => {
                                    const isQueued =
                                        run.runStatusCode ===
                                        'QUEUED'

                                    const isCancelled =
                                        run.runStatusCode ===
                                        'CANCELLED'

                                    const isCancelling =
                                        cancellingRunId ===
                                        run.analysisRunId

                                    return (
                                        <article
                                            key={
                                                run.analysisRunId
                                            }
                                            className="runs-page__run-card"
                                        >
                                            <div className="runs-page__run-heading">
                                                <div>
                                                    <div className="runs-page__run-code-row">
                                                        <h3 className="runs-page__run-code">
                                                            {
                                                                run.analysisRunCode
                                                            }
                                                        </h3>
                                                        <span className={`runs-page__run-status ${getStatusClass(run.runStatusCode)}`}>
                                                            {run.runStatusCode}
                                                        </span>
                                                    </div>

                                                    <p className="runs-page__run-source">
                                                        {run.sourceName ??
                                                            run.sourceObjectName ??
                                                            'Unknown source'}
                                                    </p>

                                                    <p className="runs-page__run-time">
                                                        Queued:{' '}
                                                        {formatLocalDateTime(
                                                            run.requestedDateTime,
                                                        )}
                                                        {run.completedDateTime && (
                                                            <> · Completed: {formatLocalDateTime(run.completedDateTime)}</>
                                                        )}
                                                    </p>

                                                    {isCancelled &&
                                                        run.cancelledDateTime && (
                                                            <p className="runs-page__run-time">
                                                                Cancelled:{' '}
                                                                {formatLocalDateTime(
                                                                    run.cancelledDateTime,
                                                                )}
                                                            </p>
                                                        )}
                                                </div>

                                                {isQueued && (
                                                    <div className="runs-page__run-actions">
                                                        <button
                                                            type="button"
                                                            className="runs-page__cancel-button"
                                                            disabled={
                                                                isCancelling
                                                            }
                                                            onClick={() =>
                                                                void handleCancelRun(
                                                                    run.analysisRunId,
                                                                )
                                                            }
                                                        >
                                                            {isCancelling
                                                                ? 'Removing...'
                                                                : 'Remove from Queue'}
                                                        </button>
                                                    </div>
                                                )}
                                            </div>

                                            <div className="runs-page__run-metrics">
                                                <div className="runs-page__metric-item">
                                                    <span className="runs-page__metric-label">Source Type</span>
                                                    <span className="runs-page__metric-value">{run.sourceTypeCode}</span>
                                                </div>

                                                <div className="runs-page__metric-item">
                                                    <span className="runs-page__metric-label">Records</span>
                                                    <span className="runs-page__metric-value">{run.totalRecordsAnalysed ?? '—'}</span>
                                                </div>

                                                <div className="runs-page__metric-item">
                                                    <span className="runs-page__metric-label">Fields Profiled</span>
                                                    <span className="runs-page__metric-value">{run.totalFieldsDiscovered ?? '—'}</span>
                                                </div>

                                                <div className="runs-page__metric-item">
                                                    <span className="runs-page__metric-label">Personal Data</span>
                                                    <span className="runs-page__metric-value runs-page__metric-value--personal">
                                                        {run.totalPersonalDataFields ?? '—'}
                                                    </span>
                                                </div>

                                                <div className="runs-page__metric-item">
                                                    <span className="runs-page__metric-label">Unclassified</span>
                                                    <span className="runs-page__metric-value">
                                                        {run.totalUnclassifiedFields ?? '—'}
                                                    </span>
                                                </div>

                                                <div className="runs-page__metric-item">
                                                    <span className="runs-page__metric-label">Coverage</span>
                                                    <span className="runs-page__metric-value runs-page__metric-value--coverage">
                                                        {run.classificationCoveragePercentage !== null && run.classificationCoveragePercentage !== undefined
                                                            ? `${run.classificationCoveragePercentage}%`
                                                            : '—'}
                                                    </span>
                                                </div>
                                            </div>
                                        </article>
                                    )
                                })}
                            </div>

                            {/* Pagination Toolbar */}
                            <div className="runs-page__pagination">
                                <span className="runs-page__pagination-info">
                                    Showing <strong>{startIndex + 1}</strong>–<strong>{Math.min(startIndex + pageSize, filteredRuns.length)}</strong> of <strong>{filteredRuns.length}</strong> runs
                                </span>

                                <div className="runs-page__pagination-controls">
                                    <button
                                        type="button"
                                        className="runs-page__page-btn"
                                        disabled={currentPage === 1}
                                        onClick={() => setCurrentPage(p => Math.max(1, p - 1))}
                                    >
                                        &larr; Previous
                                    </button>

                                    <span className="runs-page__page-indicator">
                                        Page <strong>{currentPage}</strong> of <strong>{totalPages}</strong>
                                    </span>

                                    <button
                                        type="button"
                                        className="runs-page__page-btn"
                                        disabled={currentPage >= totalPages}
                                        onClick={() => setCurrentPage(p => Math.min(totalPages, p + 1))}
                                    >
                                        Next &rarr;
                                    </button>
                                </div>
                            </div>
                        </>
                    )}
            </section>
        </section>
    )
}

export default RunsPage
