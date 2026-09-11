import {
    useState,
} from 'react'

import {
    useNavigate,
    useParams,
} from 'react-router-dom'

import {
    createAnalysisRun,
} from '../../services/runService'

import './NewRunPage.css'

type SourceTypeCode =
    | 'EXCEL'
    | 'CSV'

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

    const [isSubmitting, setIsSubmitting] =
        useState(false)

    const handleSourceTypeChange = (
        sourceType: SourceTypeCode,
    ) => {
        setSourceTypeCode(sourceType)
        setSelectedFile(null)
        setFileError(null)
        setSubmissionError(null)
    }

    const handleFileSelection = (
        file: File | null,
    ) => {
        setFileError(null)
        setSubmissionError(null)
        setSelectedFile(null)

        if (!file) {
            return
        }

        const fileName =
            file.name.toLowerCase()

        const isValidExcelFile =
            fileName.endsWith('.xlsx') ||
            fileName.endsWith('.xls')

        const isValidCsvFile =
            fileName.endsWith('.csv')

        if (
            sourceTypeCode === 'EXCEL' &&
            !isValidExcelFile
        ) {
            setFileError(
                'Please select a valid Excel file (.xlsx or .xls).',
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

    const handleStartAnalysis = async () => {
        if (
            !organisationSlug ||
            !selectedFile ||
            isSubmitting
        ) {
            return
        }

        try {
            setIsSubmitting(true)
            setSubmissionError(null)

            await createAnalysisRun(
                organisationSlug,
                {
                    sourceTypeCode,
                    sourceName: selectedFile.name,
                    sourceObjectName: null,
                },
            )

            navigate(
                `/${organisationSlug}/runs`,
            )
        } catch (error) {
            setSubmissionError(
                error instanceof Error
                    ? error.message
                    : 'An unexpected error occurred while creating the analysis run.',
            )
        } finally {
            setIsSubmitting(false)
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
                    Start a new PrivacyComply analysis run
                    against a supported local data source.
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
                            sourceTypeCode === 'EXCEL'
                                ? '.xlsx,.xls'
                                : '.csv'
                        }
                        onChange={(event) =>
                            handleFileSelection(
                                event.target.files?.[0] ??
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
                    The selected file remains local at this
                    stage. Raw customer data will not be
                    stored centrally by PrivacyComply.
                </p>

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
                        {isSubmitting
                            ? 'Creating Run...'
                            : 'Start Analysis'}
                    </button>
                </div>
            </section>
        </section>
    )
}

export default NewRunPage