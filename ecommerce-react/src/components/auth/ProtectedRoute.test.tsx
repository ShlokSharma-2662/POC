import { render, screen } from '@testing-library/react';
import {
  createMemoryRouter,
  Outlet,
  RouterProvider,
} from 'react-router-dom';
import { beforeEach, describe, expect, it } from 'vitest';
import { useAuthStore } from '../../stores/auth-store';
import { ProtectedRoute } from './ProtectedRoute';

function renderGuard(adminOnly = false) {
  const router = createMemoryRouter(
    [
      {
        path: '/',
        element: <ProtectedRoute adminOnly={adminOnly} />,
        children: [{ index: true, element: <p>Protected content</p> }],
      },
      { path: '/login', element: <p>Login page</p> },
      { path: '/unauthorized', element: <p>Unauthorized page</p> },
      { path: '*', element: <Outlet /> },
    ],
    { initialEntries: ['/'] },
  );
  render(<RouterProvider router={router} />);
}

describe('ProtectedRoute', () => {
  beforeEach(() => {
    useAuthStore.setState({
      token: null,
      user: null,
      role: null,
      isAuthenticated: false,
    });
  });

  it('redirects anonymous users to login', async () => {
    renderGuard();
    expect(await screen.findByText('Login page')).toBeInTheDocument();
  });

  it('blocks a non-admin user from admin routes', async () => {
    useAuthStore.setState({
      token: 'token',
      user: { email: 'user@example.com', role: 'User' },
      role: 'User',
      isAuthenticated: true,
    });
    renderGuard(true);
    expect(await screen.findByText('Unauthorized page')).toBeInTheDocument();
  });

  it('renders protected content for an administrator', async () => {
    useAuthStore.setState({
      token: 'token',
      user: { email: 'admin@example.com', role: 'Admin' },
      role: 'Admin',
      isAuthenticated: true,
    });
    renderGuard(true);
    expect(await screen.findByText('Protected content')).toBeInTheDocument();
  });
});
