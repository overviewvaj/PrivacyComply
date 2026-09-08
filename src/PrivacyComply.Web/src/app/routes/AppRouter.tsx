import { createBrowserRouter } from 'react-router-dom'
import AppLayout from '../../layouts/AppLayout/AppLayout'
import DashboardPage from '../../pages/Dashboard/DashboardPage'
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
        ],
    },
    {
        path: '*',
        element: <NotFoundPage />,
    },
])

export default router