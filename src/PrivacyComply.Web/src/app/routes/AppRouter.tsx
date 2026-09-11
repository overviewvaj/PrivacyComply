import {
    createBrowserRouter,
    Navigate,
} from 'react-router-dom'

import ProtectedRoute from '../../auth/ProtectedRoute'
import OrganisationRouteGuard from '../../auth/OrganisationRouteGuard'

import AppLayout from '../../layouts/AppLayout/AppLayout'

import DashboardPage from '../../pages/Dashboard/DashboardPage'
import RetentionPage from '../../pages/Retention/RetentionPage'
import SignInPage from '../../pages/SignIn/SignInPage'
import NotFoundPage from '../../pages/NotFound/NotFoundPage'

import RunsPage from '../../pages/Runs/RunsPage'
import NewRunPage from '../../pages/Runs/NewRunPage'

const router = createBrowserRouter([
    {
        path: '/',
        element: (
            <Navigate
                to="/sign-in"
                replace
            />
        ),
    },
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
                                path: 'runs',
                                element: <RunsPage />,
                            },
                            {
                                path: 'runs/new',
                                element: <NewRunPage />,
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