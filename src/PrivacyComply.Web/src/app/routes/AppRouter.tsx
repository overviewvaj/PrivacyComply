import { createBrowserRouter } from 'react-router-dom'

import ProtectedRoute from '../../auth/ProtectedRoute'
import OrganisationRouteGuard from '../../auth/OrganisationRouteGuard'

import AppLayout from '../../layouts/AppLayout/AppLayout'

import DashboardPage from '../../pages/Dashboard/DashboardPage'
import RetentionPage from '../../pages/Retention/RetentionPage'
import SignInPage from '../../pages/SignIn/SignInPage'
import NotFoundPage from '../../pages/NotFound/NotFoundPage'

const router = createBrowserRouter([
    {
        path: '/sign-in',
        element: <SignInPage />,
    },
    {
        element: <ProtectedRoute />,
        children: [
            {
                path: '/:organisationSlug',
                element: <OrganisationRouteGuard />,
                children: [
                    {
                        element: <AppLayout />,
                        children: [
                            {
                                path: 'dashboard',
                                element: <DashboardPage />,
                            },
                            {
                                path: 'retention',
                                element: <RetentionPage />,
                            },
                        ],
                    },
                ],
            },
        ],
    },
    {
        path: '*',
        element: <NotFoundPage />,
    },
])

export default router