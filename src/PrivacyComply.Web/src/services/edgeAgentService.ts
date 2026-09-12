import type {
    EdgeApiErrorResponse,
    EdgeInspectionResult,
} from '../types/EdgeInspection'


const edgeAgentBaseUrl =
    import.meta.env.VITE_EDGE_AGENT_BASE_URL

const edgeAgentTrustToken =
    import.meta.env.VITE_EDGE_AGENT_TRUST_TOKEN


function getEdgeAgentBaseUrl(): string {
    const configuredBaseUrl =
        edgeAgentBaseUrl?.trim()

    if (!configuredBaseUrl) {
        throw new Error(
            'VITE_EDGE_AGENT_BASE_URL is not configured.',
        )
    }

    return configuredBaseUrl.replace(
        /\/+$/,
        '',
    )
}


function getEdgeAgentTrustToken(): string {
    const configuredToken =
        edgeAgentTrustToken?.trim()

    if (!configuredToken) {
        throw new Error(
            'VITE_EDGE_AGENT_TRUST_TOKEN is not configured.',
        )
    }

    return configuredToken
}


async function throwEdgeAgentError(
    response: Response,
): Promise<never> {
    let errorResponse:
        EdgeApiErrorResponse | null = null

    try {
        const responseBody:
            EdgeApiErrorResponse =
            await response.json()

        errorResponse = responseBody
    } catch {
        errorResponse = null
    }

    throw new Error(
        errorResponse?.message ??
        (
            'Edge Agent request failed. ' +
            `HTTP status: ${response.status}`
        ),
    )
}


export async function getEdgeAgentHealth():
    Promise<boolean> {
    const response = await fetch(
        `${getEdgeAgentBaseUrl()}/health`,
        {
            method: 'GET',
            headers: {
                Accept: 'application/json',
            },
        },
    )

    return response.ok
}


export async function inspectLocalFile(
    file: File,
): Promise<EdgeInspectionResult> {
    const formData = new FormData()

    formData.append(
        'file',
        file,
    )

    const response = await fetch(
        (
            `${getEdgeAgentBaseUrl()}` +
            '/analysis/files/inspect'
        ),
        {
            method: 'POST',
            headers: {
                Accept: 'application/json',
                'X-PrivacyComply-Edge-Token':
                    getEdgeAgentTrustToken(),
            },
            body: formData,
        },
    )

    if (!response.ok) {
        return throwEdgeAgentError(
            response,
        )
    }

    const inspectionResult:
        EdgeInspectionResult =
        await response.json()

    return inspectionResult
}