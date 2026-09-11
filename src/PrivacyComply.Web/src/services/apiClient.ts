import {
    ApiError,
    type ApiErrorResponse,
} from '../types/ApiError'

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL

if (!apiBaseUrl) {
    throw new Error(
        'VITE_API_BASE_URL is not configured.',
    )
}

const defaultHeaders = {
    Accept: 'application/json',
}

interface AntiforgeryTokenResponse {
    requestToken: string
    headerName: string
}

async function throwApiError(
    response: Response,
    fallbackMessage: string,
): Promise<never> {
    let errorResponse: ApiErrorResponse | null = null

    try {
        errorResponse =
            (await response.json()) as ApiErrorResponse
    } catch {
        errorResponse = null
    }

    throw new ApiError(
        errorResponse?.message ?? fallbackMessage,
        response.status,
        errorResponse?.error,
    )
}

async function getAntiforgeryToken(): Promise<AntiforgeryTokenResponse> {
    const response = await fetch(
        `${apiBaseUrl}/api/security/antiforgery-token`,
        {
            method: 'GET',
            headers: defaultHeaders,
            credentials: 'include',
        },
    )

    if (!response.ok) {
        return throwApiError(
            response,
            `Failed to obtain antiforgery token. HTTP status: ${response.status}`,
        )
    }

    return response.json() as Promise<AntiforgeryTokenResponse>
}

export async function getJson<T>(
    path: string,
    fallbackMessage: string,
): Promise<T> {
    const response = await fetch(
        `${apiBaseUrl}${path}`,
        {
            method: 'GET',
            headers: defaultHeaders,
            credentials: 'include',
        },
    )

    if (!response.ok) {
        return throwApiError(
            response,
            `${fallbackMessage} HTTP status: ${response.status}`,
        )
    }

    return response.json() as Promise<T>
}

export async function postJson<
    TRequest,
    TResponse,
>(
    path: string,
    body: TRequest,
    fallbackMessage: string,
): Promise<TResponse> {
    const antiforgeryToken =
        await getAntiforgeryToken()

    const response = await fetch(
        `${apiBaseUrl}${path}`,
        {
            method: 'POST',
            headers: {
                ...defaultHeaders,
                'Content-Type': 'application/json',
                [antiforgeryToken.headerName]:
                    antiforgeryToken.requestToken,
            },
            credentials: 'include',
            body: JSON.stringify(body),
        },
    )

    if (!response.ok) {
        return throwApiError(
            response,
            `${fallbackMessage} HTTP status: ${response.status}`,
        )
    }

    if (response.status === 204) {
        return undefined as TResponse
    }

    return response.json() as Promise<TResponse>
}