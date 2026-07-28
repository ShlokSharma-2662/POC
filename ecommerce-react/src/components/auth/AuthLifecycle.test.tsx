import { act, render, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { userQueryKeys } from '../../lib/user-query';
import { useAuthStore } from '../../stores/auth-store';
import { useCartStore } from '../../stores/cart-store';
import { useWishlistStore } from '../../stores/wishlist-store';
import { AuthLifecycle } from './AuthLifecycle';

function tokenFor(subject: string): string {
  const encode = (value: unknown) =>
    btoa(JSON.stringify(value))
      .replace(/\+/g, '-')
      .replace(/\//g, '_')
      .replace(/=+$/, '');
  return `${encode({ alg: 'none' })}.${encode({
    sub: subject,
    email: `${subject}@example.com`,
    exp: Math.floor(Date.now() / 1000) + 300,
  })}.signature`;
}

const originalCartLoad = useCartStore.getState().load;
const originalWishlistLoad = useWishlistStore.getState().load;

describe('AuthLifecycle identity cache isolation', () => {
  beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    useAuthStore.setState({
      token: null,
      user: null,
      role: null,
      isAuthenticated: false,
    });
    useCartStore.setState({ load: vi.fn().mockResolvedValue(undefined) });
    useWishlistStore.setState({ load: vi.fn().mockResolvedValue(undefined) });
  });

  afterEach(() => {
    useCartStore.setState({ load: originalCartLoad });
    useWishlistStore.setState({ load: originalWishlistLoad });
  });

  it('keeps only the current subject cache and removes it on logout', async () => {
    useAuthStore.getState().setToken(tokenFor('user-a'));
    const queryClient = new QueryClient();
    queryClient.setQueryData(userQueryKeys.profile('user-a'), {
      email: 'a@example.com',
    });
    queryClient.setQueryData(userQueryKeys.profile('old-user'), {
      email: 'old@example.com',
    });

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <AuthLifecycle>
            <p>Application</p>
          </AuthLifecycle>
        </MemoryRouter>
      </QueryClientProvider>,
    );

    await waitFor(() => {
      expect(
        queryClient.getQueryData(userQueryKeys.profile('old-user')),
      ).toBeUndefined();
    });
    expect(
      queryClient.getQueryData(userQueryKeys.profile('user-a')),
    ).toEqual({ email: 'a@example.com' });

    queryClient.setQueryData(userQueryKeys.profile('user-b'), {
      email: 'b@example.com',
    });
    act(() => {
      useAuthStore.getState().setToken(tokenFor('user-b'));
    });

    await waitFor(() => {
      expect(
        queryClient.getQueryData(userQueryKeys.profile('user-a')),
      ).toBeUndefined();
    });
    expect(
      queryClient.getQueryData(userQueryKeys.profile('user-b')),
    ).toEqual({ email: 'b@example.com' });

    act(() => {
      useAuthStore.getState().logout();
    });
    await waitFor(() => {
      expect(
        queryClient.getQueryData(userQueryKeys.profile('user-b')),
      ).toBeUndefined();
    });
  });
});
