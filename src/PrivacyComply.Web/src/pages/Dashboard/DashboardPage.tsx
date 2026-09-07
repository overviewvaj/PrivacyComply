import './DashboardPage.css'

function DashboardPage() {
    return (
        <section className="dashboard-page__intro">
            <p className="dashboard-page__eyebrow">
                PrivacyComply
            </p>

            <h1 className="dashboard-page__title">
                Privacy Compliance Control Plane
            </h1>

            <p className="dashboard-page__description">
                Manage privacy compliance, governance, evidence, retention,
                data-principal rights, and regulatory workflows from one
                secure platform.
            </p>

            <div className="dashboard-page__status">
                <span
                    className="dashboard-page__status-indicator"
                    aria-hidden="true"
                />

                Frontend foundation operational
            </div>
        </section>
    )
}

export default DashboardPage