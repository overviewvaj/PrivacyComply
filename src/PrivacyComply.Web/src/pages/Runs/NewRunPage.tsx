import {
    useState,
} from 'react'

import {
    useNavigate,
    useParams,
} from 'react-router-dom'

import {
    getEdgeAgentHealth,
    inspectLocalFile,
} from '../../services/edgeAgentService'

import {
    completeAnalysisRun,
    createAnalysisRun,
    failAnalysisRun,
    startAnalysisRun,
} from '../../services/runService'

import type {
    EdgeInspectionResult,
} from '../../types/EdgeInspection'

import './NewRunPage.css'


type SourceTypeCode =
    | 'EXCEL'
    | 'CSV'


type AnalysisStage =
    | 'IDLE'
    | 'CREATING_RUN'
    | 'STARTING_RUN'
    | 'CHECKING_EDGE_AGENT'
    | 'INSPECTING_LOCAL_FILE'
    | 'COMPLETING_RUN'
    | 'FAILING_RUN'


function NewRunPage() {
    const { organisationSlug } = useParams<{
        organisationSlug: string
    }>()

    const navigate = useNavigate()

    const [sourceTypeCode, setSourceTypeCode] =
        useState<SourceTypeCode>('EXCEL')

    const [selectedFile, setSelectedFile] =
        useState<File | null>(null)

    const [fileError, setFileError] =
        useState<string | null>(null)

    const [submissionError, setSubmissionError] =
        useState<string | null>(null)

    const [
        inspectionResult,
        setInspectionResult,
    ] =
        useState<EdgeInspectionResult | null>(
            null,
        )

    const [analysisStage, setAnalysisStage] =
        useState<AnalysisStage>('IDLE')


    const isSubmitting =
        analysisStage !== 'IDLE'


    const handleSourceTypeChange = (
        sourceType: SourceTypeCode,
    ) => {
        setSourceTypeCode(sourceType)
        setSelectedFile(null)
        setFileError(null)
        setSubmissionError(null)
        setInspectionResult(null)
    }


    const handleFileSelection = (
        file: File | null,
    ) => {
        setFileError(null)
        setSubmissionError(null)
        setInspectionResult(null)
        setSelectedFile(null)

        if (!file) {
            return
        }

        const fileName =
            file.name.toLowerCase()

        const isValidExcelFile =
            fileName.endsWith('.xlsx')

        const isValidCsvFile =
            fileName.endsWith('.csv')

        if (
            sourceTypeCode === 'EXCEL' &&
            !isValidExcelFile
        ) {
            setFileError(
                'Please select a valid Excel file (.xlsx).',
            )

            return
        }

        if (
            sourceTypeCode === 'CSV' &&
            !isValidCsvFile
        ) {
            setFileError(
                'Please select a valid CSV file (.csv).',
            )

            return
        }

        setSelectedFile(file)
    }


    const getStartButtonText = (): string => {
        switch (analysisStage) {
            case 'CREATING_RUN':
                return 'Creating Run...'

            case 'STARTING_RUN':
                return 'Starting Run...'

            case 'CHECKING_EDGE_AGENT':
                return 'Checking Local Agent...'

            case 'INSPECTING_LOCAL_FILE':
                return 'Inspecting File Locally...'

            case 'COMPLETING_RUN':
                return 'Completing Run...'

            case 'FAILING_RUN':
                return 'Marking Run Failed...'

            default:
                return 'Start Analysis'
        }
    }


    const handleStartAnalysis = async () => {
        if (
            !organisationSlug ||
            !selectedFile ||
            isSubmitting
        ) {
            return
        }

        let createdAnalysisRunId: string | null =
            null

        let runStarted = false

        try {
            setSubmissionError(null)
            setInspectionResult(null)

            /*
             * Step 1:
             * Create the central analysis run first.
             *
             * Only source metadata known before
             * inspection is sent centrally.
             */
            setAnalysisStage(
                'CREATING_RUN',
            )

            const createdRun =
                await createAnalysisRun(
                    organisationSlug,
                    {
                        sourceTypeCode,
                        sourceName:
                            selectedFile.name,
                    },
                )

            createdAnalysisRunId =
                createdRun.analysisRunId

            /*
             * Step 2:
             * Transition the central run from
             * QUEUED to RUNNING.
             */
            setAnalysisStage(
                'STARTING_RUN',
            )

            await startAnalysisRun(
                organisationSlug,
                createdAnalysisRunId,
            )

            runStarted = true

            /*
             * Step 3:
             * Confirm that the local PrivacyComply
             * Edge Agent is available.
             */
            setAnalysisStage(
                'CHECKING_EDGE_AGENT',
            )

            const edgeAgentAvailable =
                await getEdgeAgentHealth()

            if (!edgeAgentAvailable) {
                throw new Error(
                    'The PrivacyComply Edge Agent is not available. ' +
                    'Please start the local Edge Agent and try again.',
                )
            }

            /*
             * Step 4:
             * Send the raw file directly from the
             * browser to the local Edge Agent.
             *
             * The file is NOT sent to
             * PrivacyComply.Api.
             */
            setAnalysisStage(
                'INSPECTING_LOCAL_FILE',
            )

            const localInspectionResult =
                await inspectLocalFile(
                    selectedFile,
                )

            if (
                localInspectionResult
                    .processingLocation !==
                'LOCAL_EDGE_AGENT'
            ) {
                throw new Error(
                    'The inspection response did not confirm local Edge Agent processing.',
                )
            }

            setInspectionResult(
                localInspectionResult,
            )

            /*
             * Step 5:
             * Send only the safe local inspection
             * summary to the central API and mark
             * the run COMPLETED.
             *
             * No raw customer data is included.
             */
            setAnalysisStage(
                'COMPLETING_RUN',
            )

            const discoveredFieldsPayload =
                (localInspectionResult.columnProfiles ?? []).map(
                    (columnProfile, index) => ({
                        sourceObjectName:
                            localInspectionResult.sheetName ?? null,
                        fieldName: columnProfile.columnName,
                        ordinalPosition: index + 1,
                        inferredDataType:
                            (columnProfile as any).inferredDataType ??
                            'TEXT',
                        classificationStatus:
                            columnProfile.classificationStatus,
                        classificationCode:
                            columnProfile.classificationCode ?? null,
                        classificationMethod:
                            columnProfile.classificationMethod,
                        matchPercentage:
                            columnProfile.matchPercentage ?? null,
                        privacyCategory:
                            columnProfile.privacyCategory ??
                            'NOT_PERSONAL',
                        isPersonalData:
                            columnProfile.isPersonalData ?? false,
                        isRegulatedIdentifier:
                            columnProfile.isRegulatedIdentifier ??
                            false,
                        nonEmptyCount:
                            (columnProfile as any).nonEmptyCount ?? 0,
                        emptyCount:
                            (columnProfile as any).emptyCount ?? 0,
                    }),
                )

            await completeAnalysisRun(
                organisationSlug,
                createdAnalysisRunId,
                {
                    sourceObjectName:
                        localInspectionResult.sheetName,

                    totalRecordsAnalysed:
                        localInspectionResult.rowCount,

                    totalFieldsDiscovered:
                        localInspectionResult.columnCount,

                    totalPersonalDataFields:
                        localInspectionResult.classificationSummary
                            .totalPersonalDataColumns ?? 0,

                    totalUnclassifiedFields:
                        localInspectionResult.classificationSummary
                            .unclassifiedColumns,

                    classificationCoveragePercentage:
                        localInspectionResult.classificationSummary
                            .classificationCoveragePercentage,

                    discoveredFields:
                        discoveredFieldsPayload,
                    findings:
                        (localInspectionResult.findings ?? []).map(
                            (finding) => ({
                                findingCode:
                                    finding.findingCode,
                                findingCategory:
                                    (finding as any).findingCategory ??
                                    finding.category ??
                                    'DATA_QUALITY',
                                severity:
                                    finding.severity,
                                fieldName:
                                    (finding as any).columnName ?? null,
                                ruleReference:
                                    null,
                                message:
                                    (finding as any).message ?? null,
                                safeMetadataJson:
                                    null,
                            }),
                        ),
                    evidence:
                        (localInspectionResult.evidence ?? []).map(
                            (ev) => ({
                                evidenceReference:
                                    ev.evidenceReference,
                                evidenceTypeCode:
                                    ev.evidenceType,
                                sourceTypeCode:
                                    'EDGE_AGENT',
                                sourceReference:
                                    ev.sourceReference,
                                fieldName:
                                    ev.fieldName ?? null,
                                classificationCode:
                                    ev.classificationCode ?? null,
                                ruleVersion:
                                    ev.ruleVersion,
                                agentVersion:
                                    ev.agentVersion,
                                evidenceHash:
                                    ev.evidenceHash,
                                hashAlgorithmCode:
                                    ev.hashAlgorithm,
                                metadataJson:
                                    ev.metadataJson,
                            }),
                        ),
                },
            )

            navigate(
                `/${organisationSlug}/runs`,
            )
        } catch (error) {
            /*
             * If a central run exists and was
             * successfully moved to RUNNING,
             * attempt to move it to FAILED.
             *
             * The failure code is intentionally
             * generic. Raw exception text and
             * customer data are not persisted.
             */
            if (
                createdAnalysisRunId &&
                runStarted
            ) {
                try {
                    setAnalysisStage(
                        'FAILING_RUN',
                    )

                    await failAnalysisRun(
                        organisationSlug,
                        createdAnalysisRunId,
                        'LOCAL_ANALYSIS_FAILED',
                    )
                } catch {
                    /*
                     * Preserve the original error
                     * shown to the user.
                     *
                     * Failure-transition errors are
                     * not used to replace it.
                     */
                }
            }

            setSubmissionError(
                error instanceof Error
                    ? error.message
                    : (
                        'An unexpected error occurred ' +
                        'while starting the analysis run.'
                    ),
            )
        } finally {
            setAnalysisStage('IDLE')
        }
    }


    return (
        <section className="new-run-page">
            <header className="new-run-page__header">
                <p className="new-run-page__eyebrow">
                    Data Analysis
                </p>

                <h1 className="new-run-page__title">
                    New Run
                </h1>

                <p className="new-run-page__description">
                    Start a new PrivacyComply
                    analysis run against a
                    supported local data source.
                </p>
            </header>

            <section className="new-run-page__panel">
                <div className="new-run-page__field">
                    <label
                        htmlFor="sourceTypeCode"
                        className="new-run-page__label"
                    >
                        Source type
                    </label>

                    <select
                        id="sourceTypeCode"
                        className="new-run-page__select"
                        value={sourceTypeCode}
                        disabled={isSubmitting}
                        onChange={(event) =>
                            handleSourceTypeChange(
                                event.target
                                    .value as SourceTypeCode,
                            )
                        }
                    >
                        <option value="EXCEL">
                            Excel
                        </option>

                        <option value="CSV">
                            CSV
                        </option>
                    </select>
                </div>

                <div className="new-run-page__field">
                    <label
                        htmlFor="sourceFile"
                        className="new-run-page__label"
                    >
                        Select source file
                    </label>

                    <input
                        key={sourceTypeCode}
                        id="sourceFile"
                        type="file"
                        className="new-run-page__file-input"
                        disabled={isSubmitting}
                        accept={
                            sourceTypeCode ===
                                'EXCEL'
                                ? '.xlsx'
                                : '.csv'
                        }
                        onChange={(event) =>
                            handleFileSelection(
                                event.target
                                    .files?.[0] ??
                                null,
                            )
                        }
                    />

                    {fileError && (
                        <p className="new-run-page__error">
                            {fileError}
                        </p>
                    )}

                    {selectedFile && (
                        <div className="new-run-page__selected-file">
                            <span>
                                Selected file
                            </span>

                            <strong>
                                {selectedFile.name}
                            </strong>

                            <span>
                                {(
                                    selectedFile.size /
                                    1024
                                ).toFixed(2)}{' '}
                                KB
                            </span>
                        </div>
                    )}
                </div>

                <div className="new-run-page__context">
                    <span>
                        Organisation
                    </span>

                    <strong>
                        {organisationSlug}
                    </strong>
                </div>

                <p className="new-run-page__notice">
                    The selected file is inspected
                    locally by the PrivacyComply
                    Edge Agent. Raw customer data
                    is not uploaded to or stored by
                    the central PrivacyComply API.
                </p>

                {inspectionResult && (
                    <div className="new-run-page__context">
                        <span>
                            Local inspection
                        </span>

                        <strong>
                            {
                                inspectionResult
                                    .rowCount
                            }{' '}
                            rows ·{' '}
                            {
                                inspectionResult
                                    .columnCount
                            }{' '}
                            columns
                        </strong>
                    </div>
                )}

                {submissionError && (
                    <p className="new-run-page__error">
                        {submissionError}
                    </p>
                )}

                <div className="new-run-page__actions">
                    <button
                        type="button"
                        className="new-run-page__start-button"
                        disabled={
                            !selectedFile ||
                            isSubmitting
                        }
                        onClick={
                            handleStartAnalysis
                        }
                    >
                        {getStartButtonText()}
                    </button>
                </div>
            </section>
        </section>
    )
}


export default NewRunPage