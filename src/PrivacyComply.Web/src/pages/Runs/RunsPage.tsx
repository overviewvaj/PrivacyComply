import {
    useCallback,
    useEffect,
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

                const result =
                    await getAnalysisRuns(
                        organisationSlug,
                    )

                setRuns(result)
            } catch (error) {
                setErrorMessage(
                    error instanceof Error
                        ? error.message
                        : 'An unexpected error occurred while loading analysis runs.',
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
            cancellingRunId
        ) {
            return
        }

        try {
            setCancellingRunId(
                analysisRunId,
            )

            setErrorMessage(null)

            await cancelAnalysisRun(
                organisationSlug,
                analysisRunId,
            )

            await loadRuns()
        } catch (error) {
            setErrorMessage(
                error instanceof Error
                    ? error.message
                    : 'An unexpected error occurred while cancelling the analysis run.',
            )
        } finally {
            setCancellingRunId(null)
        }
    }

    return (
        <section className="runs-page">
            <header className="runs-page__header">
                <div>
                    <p className="runs-page__eyebrow">
                        Data Analysis
                    </p>

                    <h1 className="runs-page__title">
                        Runs
                    </h1>

                    <p className="runs-page__description">
                        Initiate and monitor PrivacyComply
                        analysis runs across supported data
                        sources.
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
                <h2 className="runs-page__panel-title">
                    Analysis run history
                </h2>

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
                    runs.length === 0 && (
                        <p className="runs-page__message">
                            No analysis runs are currently
                            available.
                        </p>
                    )}

                {!isLoading &&
                    runs.length > 0 && (
                        <div className="runs-page__list">
                            {runs.map((run) => {
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
                                                <h3 className="runs-page__run-code">
                                                    {
                                                        run.analysisRunCode
                                                    }
                                                </h3>

                                                <p className="runs-page__run-time">
                                                    Queued:{' '}
                                                    {formatLocalDateTime(
                                                        run.requestedDateTime,
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

                                                <p className="runs-page__run-source">
                                                    {run.sourceName ??
                                                        run.sourceObjectName ??
                                                        'Unknown source'}
                                                </p>
                                            </div>

                                            <span className="runs-page__run-status">
                                                {
                                                    run.runStatusCode
                                                }
                                            </span>
                                        </div>

                                        <div className="runs-page__run-metrics">
                                            <span>
                                                Source type:{' '}
                                                {
                                                    run.sourceTypeCode
                                                }
                                            </span>

                                            <span>
                                                Records:{' '}
                                                {run.totalRecordsAnalysed ??
                                                    '—'}
                                            </span>

                                            <span>
                                                Fields:{' '}
                                                {run.totalFieldsDiscovered ??
                                                    '—'}
                                            </span>

                                            <span>
                                                Personal-data fields:{' '}
                                                {run.totalPersonalDataFields ??
                                                    '—'}
                                            </span>

                                            <span>
                                                Unclassified fields:{' '}
                                                {run.totalUnclassifiedFields ??
                                                    '—'}
                                            </span>

                                            <span>
                                                Classification coverage:{' '}
                                                {run.classificationCoveragePercentage !==
                                                    null
                                                    ? `${run.classificationCoveragePercentage}%`
                                                    : '—'}
                                            </span>
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
                                    </article>
                                )
                            })}
                        </div>
                    )}
            </section>
        </section>
    )
}

export default RunsPage