import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'

import {
    getRetentionCoverage,
    getRetentionPolicies,
} from '../../services/retentionService'

import type { RetentionCoverage } from '../../types/RetentionCoverage'
import type { RetentionPolicy } from '../../types/RetentionPolicy'

import './RetentionPage.css'

function RetentionPage() {
    const { organisationSlug } = useParams<{
        organisationSlug: string
    }>()

    const [policies, setPolicies] = useState<RetentionPolicy[]>([])
    const [coverage, setCoverage] = useState<RetentionCoverage[]>([])
    const [isLoading, setIsLoading] = useState(true)
    const [errorMessage, setErrorMessage] = useState<string | null>(null)

    useEffect(() => {
        async function loadRetentionData() {
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

                const [policyResult, coverageResult] =
                    await Promise.all([
                        getRetentionPolicies(organisationSlug),
                        getRetentionCoverage(organisationSlug),
                    ])

                setPolicies(policyResult)
                setCoverage(coverageResult)
            } catch (error) {
                setErrorMessage(
                    error instanceof Error
                        ? error.message
                        : 'An unexpected error occurred while loading retention data.',
                )
            } finally {
                setIsLoading(false)
            }
        }

        void loadRetentionData()
    }, [organisationSlug])

    return (
        <section className="retention-page">
            <header className="retention-page__header">
                <p className="retention-page__eyebrow">
                    Data Lifecycle Governance
                </p>

                <h1 className="retention-page__title">
                    Retention
                </h1>

                <p className="retention-page__description">
                    Manage retention policies, review coverage,
                    evaluate retention obligations, and track
                    retention-related compliance activity.
                </p>
            </header>

            <section className="retention-page__panel">
                <h2 className="retention-page__panel-title">
                    Retention policies
                </h2>

                {isLoading && (
                    <p className="retention-page__panel-description">
                        Loading retention data...
                    </p>
                )}

                {errorMessage && (
                    <p className="retention-page__error">
                        {errorMessage}
                    </p>
                )}

                {!isLoading &&
                    !errorMessage &&
                    policies.length === 0 && (
                        <p className="retention-page__panel-description">
                            No retention policies are currently available.
                        </p>
                    )}

                {!isLoading &&
                    !errorMessage &&
                    policies.length > 0 && (
                        <div className="retention-page__policy-list">
                            {policies.map((policy) => (
                                <article
                                    key={policy.retentionPolicyId}
                                    className="retention-page__policy-card"
                                >
                                    <h3 className="retention-page__policy-name">
                                        {policy.retentionPolicyName}
                                    </h3>

                                    <p className="retention-page__policy-code">
                                        {policy.retentionPolicyCode}
                                    </p>

                                    <div className="retention-page__policy-details">
                                        <span className="retention-page__policy-detail">
                                            {policy.retentionPeriodValue}{' '}
                                            {
                                                policy.retentionPeriodUnitCode
                                            }
                                        </span>

                                        <span className="retention-page__policy-detail">
                                            Trigger:{' '}
                                            {
                                                policy.retentionTriggerCode
                                            }
                                        </span>

                                        <span className="retention-page__policy-detail">
                                            Expiry:{' '}
                                            {
                                                policy.expiryActionCode
                                            }
                                        </span>

                                        <span
                                            className="
                                                retention-page__policy-detail
                                                retention-page__policy-status
                                            "
                                        >
                                            {policy.statusCode}
                                        </span>
                                    </div>
                                </article>
                            ))}
                        </div>
                    )}
            </section>

            {!isLoading && !errorMessage && (
                <section className="retention-page__panel">
                    <h2 className="retention-page__panel-title">
                        Retention coverage
                    </h2>

                    {coverage.length === 0 && (
                        <p className="retention-page__panel-description">
                            No processing activities are currently
                            available for retention coverage review.
                        </p>
                    )}

                    {coverage.length > 0 && (
                        <div className="retention-page__policy-list">
                            {coverage.map((item) => (
                                <article
                                    key={item.processingActivityId}
                                    className="retention-page__policy-card"
                                >
                                    <h3 className="retention-page__policy-name">
                                        {
                                            item.processingActivityName
                                        }
                                    </h3>

                                    <p className="retention-page__policy-code">
                                        {
                                            item.processingActivityCode
                                        }
                                    </p>

                                    <div className="retention-page__policy-details">
                                        <span
                                            className="
                                                retention-page__policy-detail
                                                retention-page__coverage-status
                                            "
                                        >
                                            Coverage:{' '}
                                            {item.hasRetentionPolicy
                                                ? 'Retention policy assigned'
                                                : 'No retention policy assigned'}
                                        </span>

                                        <span className="retention-page__policy-detail">
                                            Policy:{' '}
                                            {item.retentionPolicyName ??
                                                'No retention policy assigned'}
                                        </span>

                                        {item.retentionPolicyCode && (
                                            <span className="retention-page__policy-detail">
                                                {
                                                    item.retentionPolicyCode
                                                }
                                            </span>
                                        )}
                                    </div>
                                </article>
                            ))}
                        </div>
                    )}
                </section>
            )}
        </section>
    )
}

export default RetentionPage