import { beforeEach, describe, expect, it, vi } from 'vitest';
import { api, ApiError } from './api-client';
import { storageKeys } from './storage';

describe('API client', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    localStorage.clear();
  });

  it('attaches the bearer token and unwraps the API response envelope', async () => {
    localStorage.setItem(storageKeys.authToken, 'test-token');
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(
        JSON.stringify({
          isSuccessful: true,
          status: 'Success',
          statusReason: '',
          data: { productId: 10 },
        }),
        { status: 200, headers: { 'Content-Type': 'application/json' } },
      ),
    );
    vi.stubGlobal('fetch', fetchMock);

    await expect(api.get<{ productId: number }>('/products/10')).resolves.toEqual({
      productId: 10,
    });

    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(new Headers(init.headers).get('Authorization')).toBe(
      'Bearer test-token',
    );
  });

  it('surfaces API failures using the status reason', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        new Response(
          JSON.stringify({
            isSuccessful: false,
            status: 'ValidationError',
            statusReason: 'Quantity exceeds available stock.',
            data: null,
          }),
          { status: 400, headers: { 'Content-Type': 'application/json' } },
        ),
      ),
    );

    await expect(api.post('/cart', { productId: 1, quantity: 999 })).rejects.toMatchObject({
      status: 400,
      message: 'Quantity exceeds available stock.',
    } satisfies Partial<ApiError>);
  });

  it('supports the wishlist DELETE request body', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValue(new Response(null, { status: 204 }));
    vi.stubGlobal('fetch', fetchMock);

    await api.delete('/wishlist/remove', { productId: 7, userId: 0 });

    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(init.method).toBe('DELETE');
    expect(init.body).toBe(JSON.stringify({ productId: 7, userId: 0 }));
  });
});
