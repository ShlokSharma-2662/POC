import { isRouteErrorResponse, Link, useRouteError } from 'react-router-dom';

export function RouteErrorBoundary() {
  const error = useRouteError();
  const status = isRouteErrorResponse(error) ? error.status : 500;
  const message = isRouteErrorResponse(error)
    ? String(error.data || error.statusText)
    : error instanceof Error
      ? error.message
      : 'An unexpected error occurred.';

  return (
    <main className="page">
      <div className="container">
        <section className="surface empty-state" role="alert">
          <p className="page-subtitle">Error {status}</p>
          <h1>We couldn’t load this page</h1>
          <p>{message}</p>
          <Link className="btn btn-primary" to="/home">
            Return home
          </Link>
        </section>
      </div>
    </main>
  );
}
