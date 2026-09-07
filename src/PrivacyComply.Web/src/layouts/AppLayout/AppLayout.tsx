import type { ReactNode } from 'react'
import './AppLayout.css'

type AppLayoutProps = {
    children: ReactNode
}

function AppLayout({ children }: AppLayoutProps) {
    return (
        <div className="app-layout">
            <header className="app-layout__header">
                <div className="app-layout__brand">
                    <span className="app-layout__brand-mark">PC</span>

                    <div>
                        <p className="app-layout__brand-name">PrivacyComply</p>
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
                        <span className="app-layout__navigation-placeholder">
                            Navigation
                        </span>
                    </nav>
                </aside>

                <main className="app-layout__main">
                    <div className="app-layout__content">
                        {children}
                    </div>
                </main>
            </div>
        </div>
    )
}

export default AppLayout