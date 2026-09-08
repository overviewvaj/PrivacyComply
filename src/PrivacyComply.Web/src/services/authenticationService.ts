import {
    getJson,
    postJson,
} from './apiClient'

import type {
    SignInRequest,
    SignInResponse,
} from '../types/authentication'

import type {
    AuthenticationSessionResponse,
} from '../types/authenticationSession'

export async function signIn(
    request: SignInRequest,
): Promise<SignInResponse> {
    return postJson<SignInRequest, SignInResponse>(
        '/api/auth/sign-in',
        request,
        'Unable to sign in.',
    )
}

export async function getCurrentSession(
): Promise<AuthenticationSessionResponse> {
    return getJson<AuthenticationSessionResponse>(
        '/api/auth/session',
        'Unable to restore the authenticated session.',
    )
}

export async function signOut(): Promise<void> {
    await postJson<Record<string, never>, void>(
        '/api/auth/sign-out',
        {},
        'Unable to sign out.',
    )
}