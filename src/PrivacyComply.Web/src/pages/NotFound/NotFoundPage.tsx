import ErrorLayout from '../../layouts/ErrorLayout/ErrorLayout'

function NotFoundPage() {
    return (
        <ErrorLayout
            code="404"
            title="Page not found"
            message="The page you requested could not be found or may no longer be available."
        />
    )
}

export default NotFoundPage