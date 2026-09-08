import {
    NavLink,
    Outlet,
    useNavigate,
} from 'react-router-dom'

import {
    useState,
} from 'react'

import { navigationItems } from '../../app/navigation/navigationItems'
import { useAuthentication } from '../../auth/useAuthentication'

import './AppLayout.css'

function AppLayout() {
    const navigate = useNavigate()

    const {
        session,
        signOut,
    } = useAuthentication()

    const [isSigningOut, setIsSigningOut] =
        useState(false)

    async function handleSignOut() {
        if (isSigningOut) {
            return
        }

        setIsSigningOut(true)

        try {
            await signOut()

            navigate(
                '/sign-in',
                {
                    replace: true,
                },
            )
        } finally {
            setIsSigningOut(false)
        }
    }

    return (
        <div className="app-layout">
            <header className="app-layout__header">
                <div className="app-layout__brand">
                    <span className="app-layout__brand-mark">
                        PC
                    </span>

                    <div>
                        <p className="app-layout__brand-name">
                            PrivacyComply
                        </p>

                        <p className="app-layout__brand-caption">
                            Privacy Compliance Control Plane
                        </p>
                    </div>
                </div>
            </header>

            <div className="app-layout__body">
                <aside className="app-layout__sidebar">
                    <nav
                        className="app-layout__navigation"
                        aria-label="Primary navigation"
                    >
                        {navigationItems.map((item) => (
                            <NavLink
                                key={item.id}
                                to={item.path}
                                className={({ isActive }) =>
                                    isActive
                                        ? 'app-layout__navigation-link app-layout__navigation-link--active'
                                        : 'app-layout__navigation-link'
                                }
                            >
                                {item.label}
                            </NavLink>
                        ))}
                    </nav>

                    {session && (
                        <div className="app-layout__account">
                            <div className="app-layout__account-details">
                                <span className="app-layout__account-name">
                                    {session.displayName}
                                </span>

                                {session.activeOrganisation && (
                                    <span className="app-layout__account-organisation">
                                        {
                                            session
                                                .activeOrganisation
                                                .organisationName
                                        }
                                    </span>
                                )}
                            </div>

                            <button
                                type="button"
                                className="app-layout__sign-out-button"
                                onClick={() => {
                                    void handleSignOut()
                                }}
                                disabled={isSigningOut}
                            >
                                {isSigningOut
                                    ? 'Signing out...'
                                    : 'Sign out'}
                            </button>
                        </div>
                    )}
                </aside>

                <main className="app-layout__main">
                    <div className="app-layout__content">
                        <Outlet />
                    </div>
                </main>
            </div>
        </div>
    )
}

export default AppLayout