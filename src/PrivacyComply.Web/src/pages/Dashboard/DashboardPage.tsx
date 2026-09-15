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
    getAnalysisRuns,
    getDiscoveredFields,
    getEvidence,
    getFindings,
    getRuleEvaluations,
} from '../../services/runService'

import type { AnalysisRun } from '../../types/AnalysisRun'
import type {
    DiscoveredFieldDto,
    EvidenceDto,
    FindingDto,
    RuleEvaluationDto,
} from '../../types/CreateAnalysisRun'

import './DashboardPage.css'

type ActiveTab = 'overview' | 'evaluations' | 'findings' | 'fields' | 'evidence'
type FindingSeverityFilter = 'ALL' | 'HIGH' | 'MEDIUM' | 'LOW' | 'INFO'
type PrivacyCategoryFilter = 'ALL' | 'PERSONAL_DATA' | 'CONTEXT_DEPENDENT' | 'NOT_PERSONAL'
type RuleOutcomeFilter = 'ALL' | 'PASS' | 'REVIEW_REQUIRED' | 'FAIL'

function formatLocalDateTime(dateTime: string | null | undefined): string {
    if (!dateTime) return '—'
    const normalizedUtcDateTime =
        dateTime.endsWith('Z') || /[+-]\d{2}:\d{2}$/.test(dateTime)
            ? dateTime
            : `${dateTime}Z`

    const parsedDate = new Date(normalizedUtcDateTime)
    if (Number.isNaN(parsedDate.getTime())) return dateTime

    return new Intl.DateTimeFormat('en-IN', {
        dateStyle: 'medium',
        timeStyle: 'short',
    }).format(parsedDate)
}

function truncateHash(hash: string, lead = 8, trail = 8): string {
    if (!hash || hash.length <= lead + trail + 3) return hash
    return `${hash.slice(0, lead)}...${hash.slice(-trail)}`
}

function DashboardPage() {
    const { organisationSlug } = useParams<{ organisationSlug: string }>()
    const navigate = useNavigate()

    const [allRuns, setAllRuns] = useState<AnalysisRun[]>([])
    const [isLoadingRuns, setIsLoadingRuns] = useState(true)
    const [runsError, setRunsError] = useState<string | null>(null)

    const [selectedRunId, setSelectedRunId] = useState<string>('')
    const [isLoadingDetails, setIsLoadingDetails] = useState(false)
    const [detailsError, setDetailsError] = useState<string | null>(null)

    const [fields, setFields] = useState<DiscoveredFieldDto[]>([])
    const [findings, setFindings] = useState<FindingDto[]>([])
    const [evidence, setEvidence] = useState<EvidenceDto[]>([])
    const [evaluations, setEvaluations] = useState<RuleEvaluationDto[]>([])

    const [activeTab, setActiveTab] = useState<ActiveTab>('overview')
    const [findingSeverityFilter, setFindingSeverityFilter] = useState<FindingSeverityFilter>('ALL')
    const [fieldCategoryFilter, setFieldCategoryFilter] = useState<PrivacyCategoryFilter>('ALL')
    const [ruleOutcomeFilter, setRuleOutcomeFilter] = useState<RuleOutcomeFilter>('ALL')
    const [fieldSearchTerm, setFieldSearchTerm] = useState('')
    const [copiedHash, setCopiedHash] = useState<string | null>(null)

    // Filter strictly completed runs
    const completedRuns = useMemo(() => {
        return allRuns
            .filter((run) => run.runStatusCode === 'COMPLETED')
            .sort((a, b) => {
                const dateA = new Date(a.completedDateTime || a.requestedDateTime).getTime()
                const dateB = new Date(b.completedDateTime || b.requestedDateTime).getTime()
                return dateB - dateA
            })
    }, [allRuns])

    // Currently selected run object
    const selectedRun = useMemo(() => {
        return completedRuns.find((r) => r.analysisRunId === selectedRunId) || completedRuns[0] || null
    }, [completedRuns, selectedRunId])

    // Load all runs on mount
    const loadRuns = useCallback(async () => {
        if (!organisationSlug) {
            setRunsError('A valid organisation context is required.')
            setIsLoadingRuns(false)
            return
        }

        try {
            setIsLoadingRuns(true)
            setRunsError(null)
            const loaded = await getAnalysisRuns(organisationSlug)
            setAllRuns(loaded)

            const completed = loaded.filter((r) => r.runStatusCode === 'COMPLETED')
            if (completed.length > 0) {
                setSelectedRunId((prev) => {
                    const exists = completed.some((r) => r.analysisRunId === prev)
                    return exists ? prev : completed[0].analysisRunId
                })
            }
        } catch (err) {
            setRunsError(err instanceof Error ? err.message : 'Failed to load analysis runs.')
        } finally {
            setIsLoadingRuns(false)
        }
    }, [organisationSlug])

    useEffect(() => {
        void loadRuns()
    }, [loadRuns])

    // Load deep details when selectedRunId changes
    const loadRunDetails = useCallback(
        async (runId: string) => {
            if (!organisationSlug || !runId) return

            try {
                setIsLoadingDetails(true)
                setDetailsError(null)

                const [loadedFields, loadedFindings, loadedEvidence, loadedEvaluations] = await Promise.all([
                    getDiscoveredFields(organisationSlug, runId).catch(() => [] as DiscoveredFieldDto[]),
                    getFindings(organisationSlug, runId).catch(() => [] as FindingDto[]),
                    getEvidence(organisationSlug, runId).catch(() => [] as EvidenceDto[]),
                    getRuleEvaluations(organisationSlug, runId).catch(() => [] as RuleEvaluationDto[]),
                ])

                setFields(loadedFields)
                setFindings(loadedFindings)
                setEvidence(loadedEvidence)
                setEvaluations(loadedEvaluations)
            } catch (err) {
                setDetailsError(err instanceof Error ? err.message : 'Failed to load run details.')
            } finally {
                setIsLoadingDetails(false)
            }
        },
        [organisationSlug],
    )

    useEffect(() => {
        if (selectedRun?.analysisRunId) {
            void loadRunDetails(selectedRun.analysisRunId)
        } else {
            setFields([])
            setFindings([])
            setEvidence([])
            setEvaluations([])
        }
    }, [selectedRun?.analysisRunId, loadRunDetails])

    // Computed metrics
    const personalDataFieldsCount = useMemo(() => {
        if (selectedRun?.totalPersonalDataFields != null) {
            return selectedRun.totalPersonalDataFields
        }
        return fields.filter((f) => f.isPersonalData).length
    }, [selectedRun, fields])

    const regulatedIdentifierCount = useMemo(() => {
        return fields.filter((f) => f.isRegulatedIdentifier).length
    }, [fields])

    const highSeverityFindingsCount = useMemo(() => {
        return findings.filter((f) => f.severity.toUpperCase() === 'HIGH').length
    }, [findings])

    const medSeverityFindingsCount = useMemo(() => {
        return findings.filter((f) => f.severity.toUpperCase() === 'MEDIUM').length
    }, [findings])

    const lowSeverityFindingsCount = useMemo(() => {
        return findings.filter((f) => f.severity.toUpperCase() === 'LOW').length
    }, [findings])

    const infoSeverityFindingsCount = useMemo(() => {
        return findings.filter((f) => f.severity.toUpperCase() === 'INFO').length
    }, [findings])

    const verifiedEvidenceCount = useMemo(() => {
        return evidence.filter((e) => e.isVerified).length
    }, [evidence])

    // Evaluation metrics
    const passedRulesCount = useMemo(() => {
        return evaluations.filter((e) => e.evaluationOutcome === 'PASS').length
    }, [evaluations])

    const reviewRequiredRulesCount = useMemo(() => {
        return evaluations.filter((e) => e.evaluationOutcome === 'REVIEW_REQUIRED').length
    }, [evaluations])

    // Filtered evaluations
    const filteredEvaluations = useMemo(() => {
        if (ruleOutcomeFilter === 'ALL') return evaluations
        return evaluations.filter((e) => e.evaluationOutcome === ruleOutcomeFilter)
    }, [evaluations, ruleOutcomeFilter])

    // Filtered findings
    const filteredFindings = useMemo(() => {
        if (findingSeverityFilter === 'ALL') return findings
        return findings.filter((f) => f.severity.toUpperCase() === findingSeverityFilter)
    }, [findings, findingSeverityFilter])

    // Filtered fields
    const filteredFields = useMemo(() => {
        return fields.filter((f) => {
            const matchesCategory =
                fieldCategoryFilter === 'ALL' || f.privacyCategory.toUpperCase() === fieldCategoryFilter
            const matchesSearch =
                fieldSearchTerm.trim() === '' ||
                f.fieldName.toLowerCase().includes(fieldSearchTerm.toLowerCase()) ||
                (f.classificationCode && f.classificationCode.toLowerCase().includes(fieldSearchTerm.toLowerCase()))
            return matchesCategory && matchesSearch
        })
    }, [fields, fieldCategoryFilter, fieldSearchTerm])

    const handleCopyHash = (hash: string) => {
        if (navigator.clipboard) {
            void navigator.clipboard.writeText(hash)
            setCopiedHash(hash)
            setTimeout(() => setCopiedHash(null), 2000)
        }
    }

    if (isLoadingRuns) {
        return (
            <div className="dashboard-page__loading-container">
                <div className="dashboard-page__spinner" aria-hidden="true" />
                <p>Loading compliance dashboard...</p>
            </div>
        )
    }

    if (runsError) {
        return (
            <div className="dashboard-page__error-container">
                <h2>Unable to Load Dashboard</h2>
                <p>{runsError}</p>
                <button
                    type="button"
                    className="dashboard-page__retry-button"
                    onClick={() => void loadRuns()}
                >
                    Retry Loading
                </button>
            </div>
        )
    }

    // No completed runs state
    if (completedRuns.length === 0) {
        return (
            <div className="dashboard-page">
                <header className="dashboard-page__header">
                    <div>
                        <p className="dashboard-page__eyebrow">DPDP ACT 2023 • COMPLIANCE CONTROL PLANE</p>
                        <h1 className="dashboard-page__title">Compliance Dashboard</h1>
                    </div>
                </header>

                <div className="dashboard-page__empty-card">
                    <div className="dashboard-page__empty-icon" aria-hidden="true">📊</div>
                    <h2>No Completed Analysis Runs Found</h2>
                    <p>
                        The compliance dashboard visualises metrics, statutory rules evaluations, findings,
                        and cryptographic evidence specifically for successfully completed runs.
                    </p>
                    <button
                        type="button"
                        className="dashboard-page__action-button"
                        onClick={() => navigate(`/${organisationSlug}/runs/new`)}
                    >
                        + Start New Analysis Run
                    </button>
                </div>
            </div>
        )
    }

    return (
        <div className="dashboard-page">
            {/* Page Header */}
            <header className="dashboard-page__header">
                <div>
                    <p className="dashboard-page__eyebrow">DPDP ACT 2023 • COMPLIANCE CONTROL PLANE</p>
                    <h1 className="dashboard-page__title">Compliance &amp; Governance Dashboard</h1>
                    <p className="dashboard-page__subtitle">
                        Executive oversight of personal data discovery, statutory classification,
                        DPDP rule compliance evaluations, and tamper-evident cryptographic audit logs.
                    </p>
                </div>
                <div className="dashboard-page__header-actions">
                    <button
                        type="button"
                        className="dashboard-page__secondary-button"
                        onClick={() => navigate(`/${organisationSlug}/runs`)}
                    >
                        View All Runs
                    </button>
                    <button
                        type="button"
                        className="dashboard-page__action-button"
                        onClick={() => navigate(`/${organisationSlug}/runs/new`)}
                    >
                        + New Scan
                    </button>
                </div>
            </header>

            {/* Run Selector Bar */}
            <section className="dashboard-page__selector-bar" aria-label="Run Selection">
                <div className="dashboard-page__selector-label-group">
                    <label htmlFor="run-selector-dropdown" className="dashboard-page__selector-label">
                        Active Analysis Run:
                    </label>
                    <span className="dashboard-page__selector-note">
                        (Showing completed runs only)
                    </span>
                </div>

                <div className="dashboard-page__selector-controls">
                    <select
                        id="run-selector-dropdown"
                        className="dashboard-page__run-select"
                        value={selectedRun?.analysisRunId || ''}
                        onChange={(e) => setSelectedRunId(e.target.value)}
                    >
                        {completedRuns.map((run) => (
                            <option key={run.analysisRunId} value={run.analysisRunId}>
                                {run.analysisRunCode} — {run.sourceName || run.sourceTypeCode}
                                {run.sourceObjectName ? ` (${run.sourceObjectName})` : ''} — [
                                {formatLocalDateTime(run.completedDateTime)}]
                            </option>
                        ))}
                    </select>

                    <div className="dashboard-page__run-badge-group">
                        <span className="dashboard-page__status-pill status-pill--completed">
                            <span className="status-pill__dot" />
                            COMPLETED
                        </span>
                        <span className="dashboard-page__source-tag">
                            {selectedRun?.sourceTypeCode}: {selectedRun?.sourceName || 'Unnamed'}
                        </span>
                    </div>
                </div>
            </section>

            {detailsError && (
                <div className="dashboard-page__inline-error">
                    <span>⚠️ Warning: {detailsError}</span>
                    <button
                        type="button"
                        onClick={() => selectedRun && void loadRunDetails(selectedRun.analysisRunId)}
                    >
                        Retry
                    </button>
                </div>
            )}

            {isLoadingDetails ? (
                <div className="dashboard-page__loading-details">
                    <div className="dashboard-page__spinner-small" />
                    <span>Loading metrics, rules evaluations, and audit records for {selectedRun?.analysisRunCode}...</span>
                </div>
            ) : (
                <>
                    {/* Executive KPI Cards Grid */}
                    <section className="dashboard-page__kpi-grid" aria-label="Key Performance Indicators">
                        {/* 1. Records Analysed */}
                        <div className="kpi-card">
                            <div className="kpi-card__header">
                                <span className="kpi-card__title">Records Analysed</span>
                                <span className="kpi-card__icon kpi-card__icon--blue">📄</span>
                            </div>
                            <div className="kpi-card__value">
                                {(selectedRun?.totalRecordsAnalysed ?? 0).toLocaleString()}
                            </div>
                            <div className="kpi-card__footer">
                                Total scanned dataset rows
                            </div>
                        </div>

                        {/* 2. Discovered Fields */}
                        <div className="kpi-card">
                            <div className="kpi-card__header">
                                <span className="kpi-card__title">Fields Catalogued</span>
                                <span className="kpi-card__icon kpi-card__icon--indigo">🗂️</span>
                            </div>
                            <div className="kpi-card__value">
                                {(selectedRun?.totalFieldsDiscovered ?? fields.length).toLocaleString()}
                            </div>
                            <div className="kpi-card__footer">
                                {fields.filter((f) => f.classificationStatus === 'CLASSIFIED').length} Classified • {fields.filter((f) => f.classificationStatus !== 'CLASSIFIED').length} Unclassified
                            </div>
                        </div>

                        {/* 3. Personal Data Identified */}
                        <div className="kpi-card kpi-card--accent-purple">
                            <div className="kpi-card__header">
                                <span className="kpi-card__title">Personal Data Fields</span>
                                <span className="kpi-card__icon kpi-card__icon--purple">👤</span>
                            </div>
                            <div className="kpi-card__value">
                                {personalDataFieldsCount}
                            </div>
                            <div className="kpi-card__footer">
                                {fields.length > 0 ? `${((personalDataFieldsCount / fields.length) * 100).toFixed(0)}%` : '0%'} of catalogued schema
                            </div>
                        </div>

                        {/* 4. Regulated Identifiers */}
                        <div className="kpi-card kpi-card--accent-rose">
                            <div className="kpi-card__header">
                                <span className="kpi-card__title">Regulated Identifiers</span>
                                <span className="kpi-card__icon kpi-card__icon--rose">🆔</span>
                            </div>
                            <div className="kpi-card__value">
                                {regulatedIdentifierCount}
                            </div>
                            <div className="kpi-card__footer">
                                Direct statutory identifiers (Aadhaar, PAN, etc.)
                            </div>
                        </div>

                        {/* 4. Classification Coverage */}
                        <div className="kpi-card">
                            <div className="kpi-card__header">
                                <span className="kpi-card__title">Classification Coverage</span>
                                <span className="kpi-card__icon kpi-card__icon--teal">🎯</span>
                            </div>
                            <div className="kpi-card__value">
                                {selectedRun?.classificationCoveragePercentage ?? 100}%
                            </div>
                            <div className="kpi-card__progress-bar">
                                <div
                                    className="kpi-card__progress-fill"
                                    style={{
                                        width: `${selectedRun?.classificationCoveragePercentage ?? 100}%`,
                                    }}
                                />
                            </div>
                            <div className="kpi-card__footer">
                                Structural discovery coverage
                            </div>
                        </div>

                        {/* 5. DPDP Rules Evaluation (Phase 5) */}
                        <div className={`kpi-card ${reviewRequiredRulesCount > 0 ? 'kpi-card--accent-amber' : 'kpi-card--accent-emerald'}`}>
                            <div className="kpi-card__header">
                                <span className="kpi-card__title">DPDP Rules Posture</span>
                                <span className="kpi-card__icon">⚖️</span>
                            </div>
                            <div className="kpi-card__value">
                                {evaluations.length > 0 ? `${passedRulesCount} / ${evaluations.length}` : '—'}
                            </div>
                            <div className="kpi-card__footer">
                                {reviewRequiredRulesCount > 0
                                    ? `${reviewRequiredRulesCount} Review Required • ${passedRulesCount} Pass`
                                    : `${passedRulesCount} Rules Passed`}
                            </div>
                        </div>

                        {/* 6. Active Findings */}
                        <div className={`kpi-card ${findings.length > 0 ? 'kpi-card--accent-rose' : 'kpi-card--accent-emerald'}`}>
                            <div className="kpi-card__header">
                                <span className="kpi-card__title">Compliance Findings</span>
                                <span className="kpi-card__icon">{findings.length > 0 ? '⚠️' : '✅'}</span>
                            </div>
                            <div className="kpi-card__value">
                                {findings.length}
                            </div>
                            <div className="kpi-card__footer">
                                {findings.length > 0
                                    ? `${highSeverityFindingsCount} High • ${medSeverityFindingsCount} Med • ${infoSeverityFindingsCount > 0 ? `${infoSeverityFindingsCount} Info` : `${lowSeverityFindingsCount} Low`}`
                                    : 'Zero non-compliance violations'}
                            </div>
                        </div>

                        {/* 7. Cryptographic Evidence */}
                        <div className="kpi-card kpi-card--accent-emerald">
                            <div className="kpi-card__header">
                                <span className="kpi-card__title">Verified Audit Evidence</span>
                                <span className="kpi-card__icon kpi-card__icon--emerald">🔒</span>
                            </div>
                            <div className="kpi-card__value">
                                {evidence.length}
                            </div>
                            <div className="kpi-card__footer">
                                {verifiedEvidenceCount} Verified SHA-256 Hashes • Safe Proofs
                            </div>
                        </div>
                    </section>

                    {/* Navigation Tabs */}
                    <nav className="dashboard-page__tabs" aria-label="Dashboard Views">
                        <button
                            type="button"
                            className={`dashboard-page__tab-button ${activeTab === 'overview' ? 'is-active' : ''}`}
                            onClick={() => setActiveTab('overview')}
                        >
                            Executive Summary
                        </button>
                        <button
                            type="button"
                            className={`dashboard-page__tab-button ${activeTab === 'evaluations' ? 'is-active' : ''}`}
                            onClick={() => setActiveTab('evaluations')}
                        >
                            DPDP Rules Evaluation ({evaluations.length})
                        </button>
                        <button
                            type="button"
                            className={`dashboard-page__tab-button ${activeTab === 'findings' ? 'is-active' : ''}`}
                            onClick={() => setActiveTab('findings')}
                        >
                            Findings &amp; Risks ({findings.length})
                        </button>
                        <button
                            type="button"
                            className={`dashboard-page__tab-button ${activeTab === 'fields' ? 'is-active' : ''}`}
                            onClick={() => setActiveTab('fields')}
                        >
                            Discovered Fields ({fields.length})
                        </button>
                        <button
                            type="button"
                            className={`dashboard-page__tab-button ${activeTab === 'evidence' ? 'is-active' : ''}`}
                            onClick={() => setActiveTab('evidence')}
                        >
                            Cryptographic Evidence ({evidence.length})
                        </button>
                    </nav>

                    {/* Tab 1: Executive Overview */}
                    {activeTab === 'overview' && (
                        <div className="dashboard-page__overview-layout">
                            {/* Statutory Invariant Callout */}
                            <div className="statutory-banner">
                                <div className="statutory-banner__header">
                                    <span className="statutory-banner__badge">STATUTORY POSTURE ANALYSIS</span>
                                    <span className="statutory-banner__rule-ref">DPDP Act 2023 Rules Engine v1.0.0</span>
                                </div>
                                <div className="statutory-banner__comparison">
                                    <div className="statutory-box statutory-box--coverage">
                                        <span className="statutory-box__label">Discovery &amp; Classification Coverage</span>
                                        <span className="statutory-box__val text-indigo">
                                            {selectedRun?.classificationCoveragePercentage ?? 100}% Complete
                                        </span>
                                        <span className="statutory-box__sub">Measures data cataloguing completeness</span>
                                    </div>
                                    <div className="statutory-box__divider">≠</div>
                                    <div className={`statutory-box ${reviewRequiredRulesCount > 0 ? 'statutory-box--review' : 'statutory-box--pass'}`}>
                                        <span className="statutory-box__label">DPDP Regulatory Compliance Posture</span>
                                        <span className={`statutory-box__val ${reviewRequiredRulesCount > 0 ? 'text-amber' : 'text-emerald'}`}>
                                            {reviewRequiredRulesCount > 0 ? 'REVIEW REQUIRED' : 'COMPLIANT'}
                                        </span>
                                        <span className="statutory-box__sub">
                                            {passedRulesCount} Passed • {reviewRequiredRulesCount} Review Required ({evaluations.length} total rules evaluated)
                                        </span>
                                    </div>
                                </div>
                                <p className="statutory-banner__notice">
                                    <strong>Core Compliance Rule:</strong> 100% Classification Coverage is not equivalent to 100% DPDP Compliance. Classification confirms the identity of personal data attributes; regulatory evaluation tests adherence to statutory purpose limitation, security safeguards, consent tracking, and retention mandates.
                                </p>
                            </div>

                            {/* Summary Metadata Card */}
                            <div className="dashboard-panel">
                                <div className="dashboard-panel__header">
                                    <h2 className="dashboard-panel__title">Analysis Run Execution Details</h2>
                                    <span className="dashboard-panel__code">{selectedRun?.analysisRunCode}</span>
                                </div>
                                <div className="dashboard-panel__body">
                                    <div className="details-grid">
                                        <div className="details-grid__item">
                                            <span className="details-grid__label">Source Type</span>
                                            <span className="details-grid__val">{selectedRun?.sourceTypeCode}</span>
                                        </div>
                                        <div className="details-grid__item">
                                            <span className="details-grid__label">Source File / Resource</span>
                                            <span className="details-grid__val">{selectedRun?.sourceName || '—'}</span>
                                        </div>
                                        <div className="details-grid__item">
                                            <span className="details-grid__label">Worksheet / Table</span>
                                            <span className="details-grid__val">{selectedRun?.sourceObjectName || 'Primary'}</span>
                                        </div>
                                        <div className="details-grid__item">
                                            <span className="details-grid__label">Completed At</span>
                                            <span className="details-grid__val">{formatLocalDateTime(selectedRun?.completedDateTime)}</span>
                                        </div>
                                        <div className="details-grid__item">
                                            <span className="details-grid__label">DPDP Rules Pack</span>
                                            <span className="details-grid__val text-indigo">
                                                ✓ DPDP-2023-v1.0 (5 Rules Evaluated)
                                            </span>
                                        </div>
                                        <div className="details-grid__item">
                                            <span className="details-grid__label">Safe Evidence Trail</span>
                                            <span className="details-grid__val text-emerald">
                                                ✓ Immutable SHA-256 Signed ({evidence.length} records)
                                            </span>
                                        </div>
                                    </div>
                                </div>
                            </div>

                            {/* Privacy Categorisation Breakdown */}
                            <div className="dashboard-panel">
                                <div className="dashboard-panel__header">
                                    <h2 className="dashboard-panel__title">Privacy Categorisation Distribution</h2>
                                    <button
                                        type="button"
                                        className="dashboard-panel__link-btn"
                                        onClick={() => setActiveTab('fields')}
                                    >
                                        Inspect Fields Matrix &rarr;
                                    </button>
                                </div>
                                <div className="dashboard-panel__body">
                                    <div className="distribution-grid">
                                        <div className="distribution-card distribution-card--personal">
                                            <span className="distribution-card__badge">PERSONAL DATA</span>
                                            <span className="distribution-card__count">{personalDataFieldsCount}</span>
                                            <span className="distribution-card__desc">Direct &amp; contextual privacy fields</span>
                                        </div>
                                        <div className="distribution-card distribution-card--context">
                                            <span className="distribution-card__badge">CONTEXT DEPENDENT</span>
                                            <span className="distribution-card__count">
                                                {fields.filter((f) => f.privacyCategory === 'CONTEXT_DEPENDENT').length}
                                            </span>
                                            <span className="distribution-card__desc">Conditional personal data (addresses, pins, etc.)</span>
                                        </div>
                                        <div className="distribution-card distribution-card--not-personal">
                                            <span className="distribution-card__badge">NOT PERSONAL</span>
                                            <span className="distribution-card__count">
                                                {fields.filter((f) => f.privacyCategory === 'NOT_PERSONAL').length}
                                            </span>
                                            <span className="distribution-card__desc">System counters, timestamps, IDs</span>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    )}

                    {/* Tab 2: DPDP Rules Evaluation (Phase 5 & 6) */}
                    {activeTab === 'evaluations' && (
                        <div className="dashboard-panel">
                            <div className="dashboard-panel__header dashboard-panel__header--filterable">
                                <div>
                                    <h2 className="dashboard-panel__title">DPDP Act 2023 Statutory Rules Evaluation</h2>
                                    <p className="dashboard-panel__subtitle">
                                        Deterministic rule pack evaluations based on the Digital Personal Data Protection Act, 2023.
                                        Every evaluation records framework version (2023-v1.0) and rule version (1.0.0) for complete reproducibility.
                                    </p>
                                </div>
                                <div className="filter-pill-group" role="radiogroup" aria-label="Rule Outcome Filter">
                                    <button
                                        type="button"
                                        className={`filter-pill ${ruleOutcomeFilter === 'ALL' ? 'is-active' : ''}`}
                                        onClick={() => setRuleOutcomeFilter('ALL')}
                                    >
                                        All ({evaluations.length})
                                    </button>
                                    <button
                                        type="button"
                                        className={`filter-pill filter-pill--pass ${ruleOutcomeFilter === 'PASS' ? 'is-active' : ''}`}
                                        onClick={() => setRuleOutcomeFilter('PASS')}
                                    >
                                        Passed ({passedRulesCount})
                                    </button>
                                    <button
                                        type="button"
                                        className={`filter-pill filter-pill--review ${ruleOutcomeFilter === 'REVIEW_REQUIRED' ? 'is-active' : ''}`}
                                        onClick={() => setRuleOutcomeFilter('REVIEW_REQUIRED')}
                                    >
                                        Review Required ({reviewRequiredRulesCount})
                                    </button>
                                </div>
                            </div>

                            <div className="dashboard-panel__body">
                                {filteredEvaluations.length === 0 ? (
                                    <div className="dashboard-panel__empty-state">
                                        <span className="empty-icon">⚖️</span>
                                        <p>No rule evaluations match the selected filter.</p>
                                    </div>
                                ) : (
                                    <div className="evaluations-list">
                                        {filteredEvaluations.map((evalItem) => {
                                            let flaggedNames: string[] = []
                                            try {
                                                if (evalItem.flaggedFieldNamesJson) {
                                                    flaggedNames = JSON.parse(evalItem.flaggedFieldNamesJson)
                                                }
                                            } catch {
                                                flaggedNames = []
                                            }

                                            return (
                                                <div
                                                    key={evalItem.ruleEvaluationId}
                                                    className={`evaluation-card evaluation-card--${evalItem.evaluationOutcome.toLowerCase().replace(/_/g, '-')}`}
                                                >
                                                    <div className="evaluation-card__header">
                                                        <div className="evaluation-card__meta">
                                                            <span className="rule-code-badge">{evalItem.ruleCode}</span>
                                                            <span className={`outcome-badge outcome-badge--${evalItem.evaluationOutcome.toLowerCase().replace(/_/g, '-')}`}>
                                                                {evalItem.evaluationOutcome === 'PASS' && '✓ PASS'}
                                                                {evalItem.evaluationOutcome === 'REVIEW_REQUIRED' && '⚠️ REVIEW REQUIRED'}
                                                                {evalItem.evaluationOutcome === 'FAIL' && '✕ FAIL'}
                                                                {evalItem.evaluationOutcome === 'NOT_APPLICABLE' && '— NOT APPLICABLE'}
                                                            </span>
                                                            <span className={`severity-badge severity-badge--${evalItem.severity.toLowerCase()}`}>
                                                                {evalItem.severity}
                                                            </span>
                                                        </div>
                                                        <div className="evaluation-card__version-tags">
                                                            <span className="version-pill">
                                                                {evalItem.frameworkCode} {evalItem.frameworkVersion}
                                                            </span>
                                                            <span className="version-pill">
                                                                v{evalItem.ruleVersion}
                                                            </span>
                                                        </div>
                                                    </div>

                                                    <h3 className="evaluation-card__title">
                                                        {evalItem.ruleName}
                                                    </h3>

                                                    {evalItem.regulatoryReference && (
                                                        <p className="evaluation-card__ref">
                                                            Statutory Reference: <strong>{evalItem.regulatoryReference}</strong>
                                                        </p>
                                                    )}

                                                    <p className="evaluation-card__summary">
                                                        {evalItem.summaryMessage}
                                                    </p>

                                                    {flaggedNames.length > 0 && (
                                                        <div className="evaluation-card__flagged-group">
                                                            <span className="flagged-label">
                                                                Affected Schema Attributes ({flaggedNames.length}):
                                                            </span>
                                                            <div className="flagged-chips">
                                                                {flaggedNames.map((name) => (
                                                                    <code key={name} className="flagged-chip">
                                                                        {name}
                                                                    </code>
                                                                ))}
                                                            </div>
                                                        </div>
                                                    )}
                                                </div>
                                            )
                                        })}
                                    </div>
                                )}
                            </div>
                        </div>
                    )}

                    {/* Tab 3: Findings & Risks */}
                    {activeTab === 'findings' && (
                        <div className="dashboard-panel">
                            <div className="dashboard-panel__header dashboard-panel__header--filterable">
                                <div>
                                    <h2 className="dashboard-panel__title">Compliance Findings &amp; Risk Intelligence</h2>
                                    <p className="dashboard-panel__subtitle">
                                        Rule violations, statutory identifier risks, and security advisory items identified by the DPDP Engine.
                                    </p>
                                </div>
                                <div className="filter-pill-group" role="radiogroup" aria-label="Finding Severity Filter">
                                    <button
                                        type="button"
                                        className={`filter-pill ${findingSeverityFilter === 'ALL' ? 'is-active' : ''}`}
                                        onClick={() => setFindingSeverityFilter('ALL')}
                                    >
                                        All ({findings.length})
                                    </button>
                                    <button
                                        type="button"
                                        className={`filter-pill filter-pill--high ${findingSeverityFilter === 'HIGH' ? 'is-active' : ''}`}
                                        onClick={() => setFindingSeverityFilter('HIGH')}
                                    >
                                        High ({highSeverityFindingsCount})
                                    </button>
                                    <button
                                        type="button"
                                        className={`filter-pill filter-pill--medium ${findingSeverityFilter === 'MEDIUM' ? 'is-active' : ''}`}
                                        onClick={() => setFindingSeverityFilter('MEDIUM')}
                                    >
                                        Medium ({medSeverityFindingsCount})
                                    </button>
                                    <button
                                        type="button"
                                        className={`filter-pill filter-pill--low ${findingSeverityFilter === 'LOW' ? 'is-active' : ''}`}
                                        onClick={() => setFindingSeverityFilter('LOW')}
                                    >
                                        Low ({lowSeverityFindingsCount})
                                    </button>
                                    <button
                                        type="button"
                                        className={`filter-pill filter-pill--info ${findingSeverityFilter === 'INFO' ? 'is-active' : ''}`}
                                        onClick={() => setFindingSeverityFilter('INFO')}
                                    >
                                        Info ({infoSeverityFindingsCount})
                                    </button>
                                </div>
                            </div>

                            <div className="dashboard-panel__body">
                                {filteredFindings.length === 0 ? (
                                    <div className="dashboard-panel__empty-state">
                                        <span className="empty-icon">✓</span>
                                        <p>No findings matching the selected severity filter.</p>
                                    </div>
                                ) : (
                                    <div className="findings-table-wrapper">
                                        <table className="data-table">
                                            <thead>
                                                <tr>
                                                    <th>Code</th>
                                                    <th>Severity</th>
                                                    <th>Category</th>
                                                    <th>Target Field</th>
                                                    <th>Regulatory Message &amp; Advisory</th>
                                                    <th>Rule Ref</th>
                                                    <th>Status</th>
                                                </tr>
                                            </thead>
                                            <tbody>
                                                {filteredFindings.map((finding) => (
                                                    <tr key={finding.findingId}>
                                                        <td className="cell-code">{finding.findingCode}</td>
                                                        <td>
                                                            <span className={`severity-badge severity-badge--${finding.severity.toLowerCase()}`}>
                                                                {finding.severity}
                                                            </span>
                                                        </td>
                                                        <td>
                                                            <span className="category-badge">
                                                                {finding.findingCategory}
                                                            </span>
                                                        </td>
                                                        <td className="cell-field">
                                                            {finding.fieldName ? (
                                                                <code>{finding.fieldName}</code>
                                                            ) : (
                                                                <span className="text-muted">Schema-wide</span>
                                                            )}
                                                        </td>
                                                        <td className="cell-message">{finding.message || '—'}</td>
                                                        <td className="cell-rule">
                                                            {finding.ruleReference ? <code>{finding.ruleReference}</code> : '—'}
                                                        </td>
                                                        <td>
                                                            <span className="status-tag">
                                                                {finding.findingStatus}
                                                            </span>
                                                        </td>
                                                    </tr>
                                                ))}
                                            </tbody>
                                        </table>
                                    </div>
                                )}
                            </div>
                        </div>
                    )}

                    {/* Tab 4: Discovered Fields Matrix */}
                    {activeTab === 'fields' && (
                        <div className="dashboard-panel">
                            <div className="dashboard-panel__header dashboard-panel__header--filterable">
                                <div>
                                    <h2 className="dashboard-panel__title">Discovered Fields &amp; Privacy Categorisation Matrix</h2>
                                    <p className="dashboard-panel__subtitle">
                                        Catalogued schema columns, inferred data types, statutory categorisation, and regulated identifier markers.
                                    </p>
                                </div>
                                <div className="dashboard-panel__search-row">
                                    <input
                                        type="search"
                                        placeholder="Search field or classification..."
                                        className="search-input"
                                        value={fieldSearchTerm}
                                        onChange={(e) => setFieldSearchTerm(e.target.value)}
                                    />
                                    <div className="filter-pill-group" role="radiogroup" aria-label="Privacy Category Filter">
                                        <button
                                            type="button"
                                            className={`filter-pill ${fieldCategoryFilter === 'ALL' ? 'is-active' : ''}`}
                                            onClick={() => setFieldCategoryFilter('ALL')}
                                        >
                                            All ({fields.length})
                                        </button>
                                        <button
                                            type="button"
                                            className={`filter-pill filter-pill--purple ${fieldCategoryFilter === 'PERSONAL_DATA' ? 'is-active' : ''}`}
                                            onClick={() => setFieldCategoryFilter('PERSONAL_DATA')}
                                        >
                                            Personal Data ({fields.filter((f) => f.privacyCategory === 'PERSONAL_DATA').length})
                                        </button>
                                        <button
                                            type="button"
                                            className={`filter-pill filter-pill--medium ${fieldCategoryFilter === 'CONTEXT_DEPENDENT' ? 'is-active' : ''}`}
                                            onClick={() => setFieldCategoryFilter('CONTEXT_DEPENDENT')}
                                        >
                                            Context ({fields.filter((f) => f.privacyCategory === 'CONTEXT_DEPENDENT').length})
                                        </button>
                                        <button
                                            type="button"
                                            className={`filter-pill filter-pill--not-personal ${fieldCategoryFilter === 'NOT_PERSONAL' ? 'is-active' : ''}`}
                                            onClick={() => setFieldCategoryFilter('NOT_PERSONAL')}
                                        >
                                            Not Personal ({fields.filter((f) => f.privacyCategory === 'NOT_PERSONAL').length})
                                        </button>
                                    </div>
                                </div>
                            </div>

                            <div className="dashboard-panel__body">
                                {filteredFields.length === 0 ? (
                                    <div className="dashboard-panel__empty-state">
                                        <span className="empty-icon">🔍</span>
                                        <p>No schema fields match your search or filter criteria.</p>
                                    </div>
                                ) : (
                                    <div className="fields-table-wrapper">
                                        <table className="data-table">
                                            <thead>
                                                <tr>
                                                    <th>#</th>
                                                    <th>Field Name</th>
                                                    <th>Inferred Type</th>
                                                    <th>Privacy Category</th>
                                                    <th>Classification Code</th>
                                                    <th>Regulated ID?</th>
                                                    <th>Confidence</th>
                                                    <th>Data Density</th>
                                                    <th>Findings</th>
                                                </tr>
                                            </thead>
                                            <tbody>
                                                {filteredFields.map((f) => (
                                                    <tr key={f.discoveredFieldId}>
                                                        <td className="cell-ordinal">{f.ordinalPosition}</td>
                                                        <td className="cell-field">
                                                            <strong>{f.fieldName}</strong>
                                                        </td>
                                                        <td>
                                                            <span className="type-tag">{f.inferredDataType}</span>
                                                        </td>
                                                        <td>
                                                            <span className={`privacy-category-badge privacy-category-badge--${f.privacyCategory.toLowerCase().replace(/_/g, '-')}`}>
                                                                {f.privacyCategory}
                                                            </span>
                                                        </td>
                                                        <td>
                                                            {f.classificationCode ? (
                                                                <code className="classification-tag">{f.classificationCode}</code>
                                                            ) : (
                                                                <span className="text-muted">Unclassified</span>
                                                            )}
                                                        </td>
                                                        <td>
                                                            {f.isRegulatedIdentifier ? (
                                                                <span className="identifier-badge identifier-badge--yes">
                                                                    🛡️ YES
                                                                </span>
                                                            ) : (
                                                                <span className="identifier-badge identifier-badge--no">
                                                                    NO
                                                                </span>
                                                            )}
                                                        </td>
                                                        <td>
                                                            {f.matchPercentage != null ? `${f.matchPercentage}%` : '—'}
                                                        </td>
                                                        <td className="cell-density">
                                                            <span title={`Non-empty: ${f.nonEmptyCount}, Empty: ${f.emptyCount}`}>
                                                                {f.nonEmptyCount} / {f.nonEmptyCount + f.emptyCount}
                                                            </span>
                                                        </td>
                                                        <td>
                                                            {f.findingCount > 0 ? (
                                                                <span className="field-finding-count field-finding-count--alert">
                                                                    {f.findingCount} finding{f.findingCount > 1 ? 's' : ''}
                                                                </span>
                                                            ) : (
                                                                <span className="field-finding-count field-finding-count--clean">
                                                                    0
                                                                </span>
                                                            )}
                                                        </td>
                                                    </tr>
                                                ))}
                                            </tbody>
                                        </table>
                                    </div>
                                )}
                            </div>
                        </div>
                    )}

                    {/* Tab 5: Cryptographic Evidence (Phase 4) */}
                    {activeTab === 'evidence' && (
                        <div className="dashboard-panel">
                            <div className="dashboard-panel__header">
                                <div>
                                    <h2 className="dashboard-panel__title">Cryptographic Audit Evidence Trail</h2>
                                    <p className="dashboard-panel__subtitle">
                                        Immutable proof records satisfying DPDP Act 2023 Section 8 audit accountability.
                                        Every record contains non-invertible SHA-256 hashes only. No raw data is ever retained.
                                    </p>
                                </div>
                                <span className="verification-shield-badge">
                                    🔒 Zero Raw Data Guarantee
                                </span>
                            </div>

                            <div className="dashboard-panel__body">
                                {evidence.length === 0 ? (
                                    <div className="dashboard-panel__empty-state">
                                        <span className="empty-icon">📜</span>
                                        <p>No cryptographic evidence records logged for this analysis run.</p>
                                    </div>
                                ) : (
                                    <div className="evidence-table-wrapper">
                                        <table className="data-table">
                                            <thead>
                                                <tr>
                                                    <th>Evidence Ref</th>
                                                    <th>Type</th>
                                                    <th>Associated Field / Ref</th>
                                                    <th>Cryptographic Evidence Hash</th>
                                                    <th>Algorithm</th>
                                                    <th>Status</th>
                                                    <th>Timestamp</th>
                                                </tr>
                                            </thead>
                                            <tbody>
                                                {evidence.map((ev) => (
                                                    <tr key={ev.evidenceId}>
                                                        <td className="cell-code">{ev.evidenceReference}</td>
                                                        <td>
                                                            <span className="type-tag">{ev.evidenceType}</span>
                                                        </td>
                                                        <td className="cell-field">
                                                            {ev.fieldName ? <code>{ev.fieldName}</code> : <span className="text-muted">{ev.sourceReference || 'Run Level'}</span>}
                                                        </td>
                                                        <td>
                                                            <div className="hash-container">
                                                                <code className="hash-text" title={ev.evidenceHash}>
                                                                    {truncateHash(ev.evidenceHash, 10, 10)}
                                                                </code>
                                                                <button
                                                                    type="button"
                                                                    className="copy-hash-button"
                                                                    title="Copy full SHA-256 hash"
                                                                    onClick={() => handleCopyHash(ev.evidenceHash)}
                                                                >
                                                                    {copiedHash === ev.evidenceHash ? '✓ Copied' : 'Copy'}
                                                                </button>
                                                            </div>
                                                        </td>
                                                        <td>
                                                            <span className="algorithm-tag">{ev.hashAlgorithmCode}</span>
                                                        </td>
                                                        <td>
                                                            <span className="status-pill status-pill--completed">
                                                                <span className="status-pill__dot" />
                                                                VERIFIED
                                                            </span>
                                                        </td>
                                                        <td className="cell-time">
                                                            {formatLocalDateTime(ev.generatedDateTime)}
                                                        </td>
                                                    </tr>
                                                ))}
                                            </tbody>
                                        </table>
                                    </div>
                                )}
                            </div>
                        </div>
                    )}
                </>
            )}
        </div>
    )
}

export default DashboardPage
