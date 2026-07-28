import { StrictMode } from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, beforeEach, vi } from 'vitest';
import { authService } from '../../services/auth-service';
import { useAuthStore } from '../../stores/auth-store';
import { OAuthCallbackPage } from './AuthPages';

function tokenWithRole(role: string) {
  const payload = btoa(
    JSON.stringify({
      exp: Math.floor(Date.now() / 1000) + 3600,
      email: 'ada@example.com',
      'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': role,
    }),
  )
    .replaceAll('+', '-')
    .replaceAll('/', '_')
    .replaceAll('=', '');
  return `header.${payload}.signature`;
}

describe('OAuthCallbackPage', () => {
  beforeEach(() => {
    useAuthStore.getState().logout();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('exchanges an opaque code once under StrictMode and stores the returned token', async () => {
    const token = tokenWithRole('User');
    const exchange = vi
      .spyOn(authService, 'exchangeOAuthCode')
      .mockResolvedValue({
        token,
        userId: 42,
        firstName: 'Ada',
        lastName: 'Lovelace',
        name: 'Ada Lovelace',
        email: 'ada@example.com',
        role: 'User',
        returnTo: '/my-orders',
      });

    render(
      <StrictMode>
        <MemoryRouter
          initialEntries={[
            '/oauth/callback?code=opaque-code&provider=microsoft',
          ]}
        >
          <Routes>
            <Route path="/oauth/callback" element={<OAuthCallbackPage />} />
            <Route path="/my-orders" element={<h1>My orders</h1>} />
          </Routes>
        </MemoryRouter>
      </StrictMode>,
    );

    expect(
      await screen.findByText('My orders', { selector: 'h1' }),
    ).toBeInTheDocument();
    expect(exchange).toHaveBeenCalledTimes(1);
    expect(exchange).toHaveBeenCalledWith('microsoft', 'opaque-code');
    await waitFor(() => {
      expect(useAuthStore.getState().token).toBe(token);
    });
  });

  it('does not accept a JWT supplied in the callback URL', async () => {
    const exchange = vi.spyOn(authService, 'exchangeOAuthCode');

    render(
      <MemoryRouter initialEntries={['/oauth/callback?jwt_token=attacker-token']}>
        <Routes>
          <Route path="/oauth/callback" element={<OAuthCallbackPage />} />
        </Routes>
      </MemoryRouter>,
    );

    expect(
      await screen.findByText('Authentication failed', { selector: 'h1' }),
    ).toBeInTheDocument();
    expect(exchange).not.toHaveBeenCalled();
    expect(useAuthStore.getState().token).toBeNull();
  });
});
