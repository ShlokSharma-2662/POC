import { beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '../lib/api-client';
import { storageKeys, writeJson } from '../lib/storage';
import type { Product, WishlistItem } from '../types/domain';
import { useAuthStore } from './auth-store';
import { useCartStore } from './cart-store';
import { useWishlistStore } from './wishlist-store';

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

function product(productId: number, stock = 10): Product {
  return {
    productId,
    name: `Product ${productId}`,
    description: 'Test product',
    price: productId * 10,
    stock,
    categoryId: 1,
  };
}

function wishlistItem(productId: number): WishlistItem {
  const itemProduct = product(productId);
  return {
    id: productId,
    productId,
    productName: itemProduct.name,
    productDescription: itemProduct.description,
    productPrice: itemProduct.price,
    stock: itemProduct.stock,
    addedAt: '2026-01-01T00:00:00.000Z',
    isInStock: true,
  };
}

describe('identity-scoped storefront state', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    localStorage.clear();
    sessionStorage.clear();
    useAuthStore.setState({
      token: null,
      user: null,
      role: null,
      isAuthenticated: false,
    });
    useCartStore.setState({
      items: [],
      loading: false,
      storageKey: storageKeys.guestCart,
    });
    useWishlistStore.setState({
      items: [],
      guestItems: [],
      loading: false,
    });
  });

  it('deduplicates and stock-caps guest cart items before merging', async () => {
    const itemProduct = product(7, 5);
    writeJson(localStorage, storageKeys.guestCart, [
      { product: itemProduct, quantity: 3, price: itemProduct.price },
      { product: itemProduct, quantity: 4, price: itemProduct.price },
    ]);
    useAuthStore.getState().setToken(tokenFor('user-a'));

    let cartRead = 0;
    vi.spyOn(api, 'get').mockImplementation(async (path) => {
      if (path !== '/cart') throw new Error(`Unexpected GET ${path}`);
      cartRead += 1;
      return (cartRead === 1
        ? [{ product: itemProduct, quantity: 2, price: itemProduct.price }]
        : [{ product: itemProduct, quantity: 5, price: itemProduct.price }]) as never;
    });
    const post = vi.spyOn(api, 'post').mockResolvedValue(undefined as never);

    await useCartStore.getState().load();

    expect(post).toHaveBeenCalledTimes(1);
    expect(post).toHaveBeenCalledWith('/cart', {
      productId: 7,
      quantity: 3,
    });
    expect(
      JSON.parse(localStorage.getItem(storageKeys.guestCart) ?? '[]'),
    ).toEqual([]);
    expect(useCartStore.getState().items).toEqual([
      expect.objectContaining({ quantity: 5 }),
    ]);
  });

  it('retains a guest cart item when its server merge fails', async () => {
    const itemProduct = product(8, 4);
    const guestCart = [
      { product: itemProduct, quantity: 2, price: itemProduct.price },
    ];
    writeJson(localStorage, storageKeys.guestCart, guestCart);
    useAuthStore.getState().setToken(tokenFor('user-a'));

    vi.spyOn(api, 'get').mockImplementation(async (path) => {
      if (path === '/cart') return [] as never;
      if (path === '/products/8') return itemProduct as never;
      throw new Error(`Unexpected GET ${path}`);
    });
    vi.spyOn(api, 'post').mockRejectedValue(new Error('Network unavailable'));

    await useCartStore.getState().load();

    expect(
      JSON.parse(localStorage.getItem(storageKeys.guestCart) ?? '[]'),
    ).toEqual(guestCart);
    expect(useCartStore.getState().items).toEqual([]);
  });

  it('does not let a late account-A cart response overwrite account B', async () => {
    let resolveFirstCart!: (value: unknown) => void;
    const firstCart = new Promise((resolve) => {
      resolveFirstCart = resolve;
    });
    let cartRead = 0;
    vi.spyOn(api, 'get').mockImplementation(async (path) => {
      if (path !== '/cart') throw new Error(`Unexpected GET ${path}`);
      cartRead += 1;
      if (cartRead === 1) return (await firstCart) as never;
      return [{ product: product(2), quantity: 1, price: 20 }] as never;
    });

    useAuthStore.getState().setToken(tokenFor('user-a'));
    const accountALoad = useCartStore.getState().load();
    useAuthStore.getState().setToken(tokenFor('user-b'));
    const accountBLoad = useCartStore.getState().load();
    resolveFirstCart([{ product: product(1), quantity: 1, price: 10 }]);

    await Promise.all([accountALoad, accountBLoad]);

    expect(useCartStore.getState().storageKey).toBe('cart_user-b');
    expect(useCartStore.getState().items).toEqual([
      expect.objectContaining({
        product: expect.objectContaining({ productId: 2 }),
      }),
    ]);
  });

  it('clears only successfully merged guest wishlist entries', async () => {
    const first = product(1);
    const second = product(2);
    const failed = product(3);
    writeJson(localStorage, storageKeys.guestWishlist, [
      { product: first, addedAt: '2026-01-01T00:00:00.000Z' },
      { product: first, addedAt: '2026-01-02T00:00:00.000Z' },
      { product: second, addedAt: '2026-01-03T00:00:00.000Z' },
      { product: failed, addedAt: '2026-01-04T00:00:00.000Z' },
    ]);
    useAuthStore.getState().setToken(tokenFor('user-a'));

    let wishlistRead = 0;
    vi.spyOn(api, 'get').mockImplementation(async (path) => {
      if (path !== '/wishlist') throw new Error(`Unexpected GET ${path}`);
      wishlistRead += 1;
      return (wishlistRead === 1
        ? [wishlistItem(1)]
        : [wishlistItem(1), wishlistItem(2)]) as never;
    });
    const post = vi.spyOn(api, 'post').mockImplementation(async (path, body) => {
      const productId = (body as { productId: number }).productId;
      if (path === '/wishlist/add' && productId === 3) {
        throw new Error('Network unavailable');
      }
      return true as never;
    });

    await useWishlistStore.getState().load();

    expect(post).toHaveBeenCalledTimes(2);
    expect(
      JSON.parse(localStorage.getItem(storageKeys.guestWishlist) ?? '[]'),
    ).toEqual([
      expect.objectContaining({
        product: expect.objectContaining({ productId: 3 }),
      }),
    ]);
    expect(useWishlistStore.getState().items.map((item) => item.productId)).toEqual([
      1, 2,
    ]);
  });
});
