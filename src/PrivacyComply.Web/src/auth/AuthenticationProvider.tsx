import {
    type PropsWithChildren,
    useCallback,
    useEffect,
    useMemo,
    useState,
} from 'react'

import { AuthenticationContext } from './AuthenticationContext'

import {
    getCurrentSession,
    signIn as executeSignIn,
    signOut as executeSignOut,
} from '../services/authenticationService'

import type {
    SignInRequest,
} from '../types/authentication'

import type {
    AuthenticationSession,
} from '../types/authenticationSession'

export function AuthenticationProvider({
    children,
}: PropsWithChildren) {
    const [session, setSession] =
        useState<AuthenticationSession | null>(null)

    const [isSigningIn, setIsSigningIn] =
        useState(false)

    const [
        isRestoringSession,
        setIsRestoringSession,
    ] = useState(true)

    // --------------------------------------------------------
    // Restore Existing Browser Session
    // --------------------------------------------------------

    useEffect(() => {
        let isCancelled = false

        async function restoreSession() {
            try {
                const response =
                    await getCurrentSession()

                if (isCancelled) {
                    return
                }

                if (
                    !response.isAuthenticated ||
                    response.resultCode !== 'SUCCESS' ||
                    !response.userAccountId ||
                    !response.displayName
                ) {
                    setSession(null)
                    return
                }

                const activeOrganisation =
                    response.organisations.find(
                        organisation =>
                            organisation.isPrimaryOrganisation,
                    ) ??
                    response.organisations[0] ??
                    null

                const restoredSession:
                    AuthenticationSession = {
                    userAccountId:
                        response.userAccountId,

                    displayName:
                        response.displayName,

                    isPlatformUser:
                        response.isPlatformUser,

                    organisations:
                        response.organisations,

                    activeOrganisation,
                }

                setSession(restoredSession)
            } catch {
                if (!isCancelled) {
                    setSession(null)
                }
            } finally {
                if (!isCancelled) {
                    setIsRestoringSession(false)
                }
            }
        }

        void restoreSession()

        return () => {
            isCancelled = true
        }
    }, [])

    // --------------------------------------------------------
    // Sign In
    // --------------------------------------------------------

    const signIn = useCallback(
        async (
            request: SignInRequest,
        ): Promise<AuthenticationSession> => {
            setIsSigningIn(true)

            try {
                const response =
                    await executeSignIn(request)

                if (
                    !response.succeeded ||
                    response.resultCode !== 'SUCCESS'
                ) {
                    throw new Error(
                        response.message ??
                        'Unable to sign in.',
                    )
                }

                if (
                    !response.userAccountId ||
                    !response.displayName ||
                    !response.accessToken ||
                    !response.tokenType ||
                    !response.expiresDateTime
                ) {
                    throw new Error(
                        'The authentication response is incomplete.',
                    )
                }

                const activeOrganisation =
                    response.organisations.find(
                        organisation =>
                            organisation.isPrimaryOrganisation,
                    ) ??
                    response.organisations[0] ??
                    null

                const authenticationSession:
                    AuthenticationSession = {
                    userAccountId:
                        response.userAccountId,

                    displayName:
                        response.displayName,

                    isPlatformUser:
                        response.isPlatformUser,

                    accessToken:
                        response.accessToken,

                    tokenType:
                        response.tokenType,

                    expiresDateTime:
                        response.expiresDateTime,

                    organisations:
                        response.organisations,

                    activeOrganisation,
                }

                setSession(authenticationSession)

                return authenticationSession
            } finally {
                setIsSigningIn(false)
            }
        },
        [],
    )

    // --------------------------------------------------------
    // Sign Out
    // --------------------------------------------------------

    const signOut = useCallback(async () => {
        try {
            // Revoke the server-side authentication session
            // and remove the HttpOnly authentication cookie.
            await executeSignOut()
        } finally {
            // Always clear the local React authentication state.
            //
            // Even if the API request fails, the current browser
            // UI must stop treating the user as authenticated.
            setSession(null)
        }
    }, [])

    // --------------------------------------------------------
    // Permission Helpers
    // --------------------------------------------------------

    const hasPermission = useCallback(
        (
            permissionCode: string,
        ): boolean => {
            if (!session?.activeOrganisation) {
                return false
            }

            return session.activeOrganisation.permissions.includes(
                permissionCode,
            )
        },
        [session],
    )

    const hasRole = useCallback(
        (
            roleCode: string,
        ): boolean => {
            if (!session?.activeOrganisation) {
                return false
            }

            return session.activeOrganisation.roles.includes(
                roleCode,
            )
        },
        [session],
    )

    // --------------------------------------------------------
    // Authentication Context
    // --------------------------------------------------------

    const contextValue = useMemo(
        () => ({
            session,

            isAuthenticated:
                session !== null,

            isSigningIn,

            isRestoringSession,

            signIn,
            signOut,
            hasPermission,
            hasRole,
        }),
        [
            session,
            isSigningIn,
            isRestoringSession,
            signIn,
            signOut,
            hasPermission,
            hasRole,
        ],
    )

    return (
        <AuthenticationContext.Provider
            value={contextValue}
        >
            {children}
        </AuthenticationContext.Provider>
    )
}