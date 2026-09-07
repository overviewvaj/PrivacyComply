import type { ReactNode } from 'react'
import './ErrorLayout.css'

type ErrorLayoutProps = {
    code?: string
    title: string
    message: string
    children?: ReactNode
}

function ErrorLayout({
    code,
    title,
    message,
    children,
}: ErrorLayoutProps) {
    return (
        <main className="error-layout">
            <section className="error-layout__panel">
                <div className="error-layout__brand">
                    <span className="error-layout__brand-mark">PC</span>

                    <div>
                        <p className="error-layout__brand-name">
                            PrivacyComply
                        </p>

                        <p className="error-layout__brand-caption">
                            Privacy Compliance Control Plane
                        </p>
                    </div>
                </div>

                <div className="error-layout__content">
                    {code && (
                        <p className="error-layout__code">
                            {code}
                        </p>
                    )}

                    <h1 className="error-layout__title">
                        {title}
                    </h1>

                    <p className="error-layout__message">
                        {message}
                    </p>

                    {children && (
                        <div className="error-layout__actions">
                            {children}
                        </div>
                    )}
                </div>
            </section>
        </main>
    )
}

export default ErrorLayout