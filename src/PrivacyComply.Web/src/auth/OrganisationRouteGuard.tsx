import { Navigate, Outlet, useParams } from 'react-router-dom'
import { useAuthentication } from './useAuthentication'

export default function OrganisationRouteGuard() {
    const { organisationSlug } = useParams()
    const { session } = useAuthentication()

    if (!session) {
        return (
            <Navigate
                to="/sign-in"
                replace
            />
        )
    }

    const organisation =
        session.organisations.find(
            item =>
                item.organisationSlug ===
                organisationSlug,
        )

    if (!organisation) {
        const fallbackOrganisation =
            session.activeOrganisation

        if (!fallbackOrganisation) {
            return (
                <Navigate
                    to="/sign-in"
                    replace
                />
            )
        }

        return (
            <Navigate
                to={`/${fallbackOrganisation.organisationSlug}/dashboard`}
                replace
            />
        )
    }

    return <Outlet />
}