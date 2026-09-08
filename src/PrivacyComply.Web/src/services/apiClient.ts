import {
    ApiError,
    type ApiErrorResponse,
} from '../types/ApiError'

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL
const developmentOrganisationId =
    import.meta.env.VITE_DEVELOPMENT_ORGANISATION_ID

if (!apiBaseUrl) {
    throw new Error('VITE_API_BASE_URL is not configured.')
}

if (!developmentOrganisationId) {
    throw new Error(
        'VITE_DEVELOPMENT_ORGANISATION_ID is not configured.',
    )
}

const defaultHeaders = {
    Accept: 'application/json',
    'X-Organisation-Id': developmentOrganisationId,
}

async function throwApiError(
    response: Response,
    fallbackMessage: string,
): Promise<never> {
    let errorResponse: ApiErrorResponse | null = null

    try {
        errorResponse = (await response.json()) as ApiErrorResponse
    } catch {
        errorResponse = null
    }

    throw new ApiError(
        errorResponse?.message ?? fallbackMessage,
        response.status,
        errorResponse?.error,
    )
}

export async function getJson<T>(
    path: string,
    fallbackMessage: string,
): Promise<T> {
    const response = await fetch(`${apiBaseUrl}${path}`, {
        method: 'GET',
        headers: defaultHeaders,
    })

    if (!response.ok) {
        return throwApiError(
            response,
            `${fallbackMessage} HTTP status: ${response.status}`,
        )
    }

    return response.json() as Promise<T>
}