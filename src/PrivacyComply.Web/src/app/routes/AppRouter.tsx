import { createBrowserRouter } from 'react-router-dom'
import AppLayout from '../../layouts/AppLayout/AppLayout'
import DashboardPage from '../../pages/Dashboard/DashboardPage'
import RetentionPage from '../../pages/Retention/RetentionPage'
import NotFoundPage from '../../pages/NotFound/NotFoundPage'

const router = createBrowserRouter([
    {
        path: '/',
        element: <AppLayout />,
        children: [
            {
                index: true,
                element: <DashboardPage />,
            },
            {
                path: 'retention',
                element: <RetentionPage />,
            },
        ],
    },
    {
        path: '*',
        element: <NotFoundPage />,
    },
])

export default router