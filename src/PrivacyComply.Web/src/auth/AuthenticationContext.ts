import { createContext } from 'react'

import type {
    AuthenticationSession,
} from '../types/authenticationSession'

import type {
    SignInRequest,
} from '../types/authentication'

export interface AuthenticationContextValue {
    session: AuthenticationSession | null

    isAuthenticated: boolean
    isSigningIn: boolean
    isRestoringSession: boolean

    signIn: (
        request: SignInRequest,
    ) => Promise<AuthenticationSession>

    signOut: () => Promise<void>

    hasPermission: (
        permissionCode: string,
    ) => boolean

    hasRole: (
        roleCode: string,
    ) => boolean
}

export const AuthenticationContext =
    createContext<
        AuthenticationContextValue | undefined
    >(undefined)