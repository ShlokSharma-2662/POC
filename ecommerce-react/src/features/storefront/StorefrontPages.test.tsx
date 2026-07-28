import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { storageKeys, writeJson } from '../../lib/storage';
import { useAuthStore } from '../../stores/auth-store';
import { UnauthorizedPage } from './AccountPages';
import { SuccessPage } from './CommercePages';
import type { LastOrderSummary } from './storefront-utils';

describe('storefront completion pages', () => {
  beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    useAuthStore.setState({
      token: 'e30.eyJzdWIiOiJ1c2VyLTEifQ.',
      isAuthenticated: true,
    });
  });

  it('shows the confirmed order and persisted item summary', () => {
    const summary: LastOrderSummary = {
      ownerSubject: 'user-1',
      orderId: 'stored-order',
      fullName: 'Ada Lovelace',
      address: '12 Computing Way',
      phoneNumber: '1234567890',
      total: 249.5,
      items: [
        {
          name: 'Analytical Headphones',
          price: 249.5,
          quantity: 1,
        },
      ],
    };
    writeJson(
      sessionStorage,
      `${storageKeys.lastOrderSummary}:user-1`,
      summary,
    );

    render(
      <MemoryRouter initialEntries={['/success?orderId=api-order-42']}>
        <Routes>
          <Route path="/success" element={<SuccessPage />} />
        </Routes>
      </MemoryRouter>,
    );

    expect(screen.getByText(/thank you for your order/i)).toBeInTheDocument();
    expect(screen.getByText('api-order-42')).toBeInTheDocument();
    expect(screen.getByText(/Analytical Headphones/)).toBeInTheDocument();
    expect(screen.getByText(/12 Computing Way/)).toBeInTheDocument();
  });

  it('does not reveal another account summary', () => {
    const summary: LastOrderSummary = {
      ownerSubject: 'other-user',
      orderId: 'private-order',
      fullName: 'Private Customer',
      address: 'Hidden address',
      phoneNumber: '1234567890',
      total: 99,
      items: [{ name: 'Private item', price: 99, quantity: 1 }],
    };
    writeJson(
      sessionStorage,
      `${storageKeys.lastOrderSummary}:user-1`,
      summary,
    );

    render(
      <MemoryRouter initialEntries={['/success']}>
        <Routes>
          <Route path="/success" element={<SuccessPage />} />
        </Routes>
      </MemoryRouter>,
    );

    expect(screen.queryByText('Private Customer')).not.toBeInTheDocument();
    expect(screen.queryByText('Private item')).not.toBeInTheDocument();
    expect(
      sessionStorage.getItem(`${storageKeys.lastOrderSummary}:user-1`),
    ).toBeNull();
  });

  it('offers safe navigation from the unauthorized page', () => {
    render(
      <MemoryRouter>
        <UnauthorizedPage />
      </MemoryRouter>,
    );

    expect(
      screen.getByText(/you don’t have permission to view this page/i),
    ).toBeInTheDocument();
    expect(screen.getByText(/go to home/i).closest('a')).toHaveAttribute(
      'href',
      '/home',
    );
    expect(
      screen.getByText('Contact support', { selector: 'a' }),
    ).toHaveAttribute('href', 'mailto:support@eshop.com');
  });
});
