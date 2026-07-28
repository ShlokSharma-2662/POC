import { act, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it } from 'vitest';
import { userQueryKeys } from '../../lib/user-query';
import { useAuthStore } from '../../stores/auth-store';
import type { StorefrontOrder } from './storefront-utils';
import { OrdersPage } from './AccountPages';

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

function order(id: string, customerName: string): StorefrontOrder {
  return {
    id,
    customerName,
    shippingAddress: `${customerName} address`,
    phone: '1234567890',
    createdAt: '2026-01-01T00:00:00.000Z',
    status: 'Pending',
    items: [
      {
        productId: 1,
        productName: `${customerName} product`,
        price: 10,
        quantity: 1,
      },
    ],
  };
}

describe('account page identity isolation', () => {
  beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    useAuthStore.setState({
      token: null,
      user: null,
      role: null,
      isAuthenticated: false,
    });
  });

  it('drops account A orders and its open detail modal after switching to B', async () => {
    const user = userEvent.setup();
    const queryClient = new QueryClient({
      defaultOptions: { queries: { staleTime: Number.POSITIVE_INFINITY } },
    });
    queryClient.setQueryData(userQueryKeys.orders('user-a'), [
      order('order-a', 'Account A'),
    ]);
    queryClient.setQueryData(userQueryKeys.orders('user-b'), [
      order('order-b', 'Account B'),
    ]);
    useAuthStore.getState().setToken(tokenFor('user-a'));

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <OrdersPage />
        </MemoryRouter>
      </QueryClientProvider>,
    );

    expect(screen.getByText('Account A')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'View details' }));
    expect(document.querySelector('[role="dialog"]')).toHaveTextContent(
      'Account A',
    );

    act(() => {
      useAuthStore.getState().setToken(tokenFor('user-b'));
    });

    await waitFor(() => {
      expect(document.querySelector('[role="dialog"]')).not.toBeInTheDocument();
    });
    expect(screen.queryByText('Account A')).not.toBeInTheDocument();
    expect(screen.getByText('Account B')).toBeInTheDocument();
  });
});
