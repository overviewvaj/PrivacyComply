import {
    Navigate,
    Outlet,
    useLocation,
} from 'react-router-dom'

import { useAuthentication } from './useAuthentication'

export default function ProtectedRoute() {
    const {
        isAuthenticated,
        isRestoringSession,
    } = useAuthentication()

    const location = useLocation()

    // --------------------------------------------------------
    // Session Restoration
    // --------------------------------------------------------

    // On a full browser refresh the React authentication state
    // starts empty.
    //
    // AuthenticationProvider first asks the backend to restore
    // the authenticated session from the secure HttpOnly cookie.
    //
    // Protected routes must wait for that process to finish
    // before deciding whether the user should be redirected.
    if (isRestoringSession) {
        return (
            <div
                role="status"
                aria-live="polite"
                aria-label="Restoring authenticated session"
            >
                Restoring session...
            </div>
        )
    }

    // --------------------------------------------------------
    // Unauthenticated
    // --------------------------------------------------------

    // Session restoration has completed and no valid
    // authenticated session exists.
    if (!isAuthenticated) {
        return (
            <Navigate
                to="/sign-in"
                replace
                state={{
                    returnTo:
                        location.pathname +
                        location.search +
                        location.hash,
                }}
            />
        )
    }

    // --------------------------------------------------------
    // Authenticated
    // --------------------------------------------------------

    return <Outlet />
}