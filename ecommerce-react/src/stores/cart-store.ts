import { create } from 'zustand';
import { api } from '../lib/api-client';
import { getSubjectFromToken } from '../lib/jwt';
import {
  getStoredToken,
  readJson,
  storageKeys,
  writeJson,
} from '../lib/storage';
import type { CartItem, Product } from '../types/domain';

interface ServerCartItem {
  product: Product;
  quantity: number;
  price?: number;
}

type ProductWithStockAlias = Product & { stockQuantity?: number };

interface CartState {
  items: CartItem[];
  loading: boolean;
  storageKey: string;
  load: () => Promise<void>;
  add: (product: Product, quantity?: number) => Promise<void>;
  updateQuantity: (productId: number, quantity: number) => Promise<void>;
  remove: (productId: number) => Promise<void>;
  clear: () => Promise<void>;
}

let cartLoadQueue: Promise<unknown> = Promise.resolve();

function enqueueCartLoad<T>(task: () => Promise<T>): Promise<T> {
  const result = cartLoadQueue.then(task, task);
  cartLoadQueue = result.then(
    () => undefined,
    () => undefined,
  );
  return result;
}

function currentSubject(): string | null {
  return getSubjectFromToken(getStoredToken());
}

function storageKeyForSubject(subject: string | null): string {
  return subject ? `cart_${subject}` : storageKeys.guestCart;
}

function currentStorageKey(): string {
  return storageKeyForSubject(currentSubject());
}

function readItems(key = currentStorageKey()): CartItem[] {
  return readJson<CartItem[]>(localStorage, key, []);
}

function persist(key: string, items: CartItem[]): void {
  writeJson(localStorage, key, items);
}

function normalizeServerItems(items: ServerCartItem[]): CartItem[] {
  return items.map((item) => ({
    product: item.product,
    quantity: Math.max(0, Math.floor(Number(item.quantity) || 0)),
    price: Number(item.price ?? item.product.price) || 0,
  }));
}

function dedupeCartItems(items: CartItem[]): CartItem[] {
  const deduped = new Map<number, CartItem>();

  for (const item of items) {
    const productId = Number(item.product?.productId);
    const quantity = Math.max(0, Math.floor(Number(item.quantity) || 0));
    if (!productId || quantity === 0) continue;

    const existing = deduped.get(productId);
    deduped.set(productId, {
      product: item.product,
      quantity: (existing?.quantity ?? 0) + quantity,
      price: Number(item.price ?? item.product.price) || 0,
    });
  }

  return [...deduped.values()];
}

function removeMergedGuestCartItem(productId: number): void {
  const remaining = readItems(storageKeys.guestCart).filter(
    (item) => item.product.productId !== productId,
  );
  persist(storageKeys.guestCart, remaining);
}

function productStock(product: ProductWithStockAlias): number {
  const stock = Number(product.stock ?? product.stockQuantity ?? 0);
  return Number.isFinite(stock) ? Math.max(0, Math.floor(stock)) : 0;
}

async function mergeGuestCart(
  subject: string,
  serverItems: CartItem[],
): Promise<CartItem[]> {
  const byProductId = new Map(
    serverItems.map((item) => [item.product.productId, item]),
  );
  const guestItems = dedupeCartItems(readItems(storageKeys.guestCart));

  for (const guestItem of guestItems) {
    if (currentSubject() !== subject) break;

    const productId = guestItem.product.productId;
    const existing = byProductId.get(productId);
    let product = existing?.product;

    if (!product) {
      try {
        product = await api.get<Product>(`/products/${productId}`);
      } catch {
        continue;
      }
      if (currentSubject() !== subject) break;
    }

    const currentQuantity = existing?.quantity ?? 0;
    const addableQuantity = Math.max(
      0,
      productStock(product) - currentQuantity,
    );
    const quantityToAdd = Math.min(guestItem.quantity, addableQuantity);

    if (quantityToAdd === 0) {
      if (currentQuantity > 0) {
        removeMergedGuestCartItem(productId);
      }
      continue;
    }

    try {
      await api.post('/cart', { productId, quantity: quantityToAdd });
      removeMergedGuestCartItem(productId);
      byProductId.set(productId, {
        product,
        quantity: currentQuantity + quantityToAdd,
        price: Number(product.price) || guestItem.price,
      });
    } catch {
      // The guest item remains durable and can be retried on the next load.
    }
  }

  return [...byProductId.values()];
}

async function loadAuthenticatedCart(
  subject: string,
): Promise<CartItem[] | null> {
  if (currentSubject() !== subject) return null;

  const serverItems = normalizeServerItems(
    (await api.get<ServerCartItem[]>('/cart')) ?? [],
  );
  if (currentSubject() !== subject) return null;

  const mergedItems = await mergeGuestCart(subject, serverItems);
  if (currentSubject() !== subject) return null;

  try {
    return normalizeServerItems(
      (await api.get<ServerCartItem[]>('/cart')) ?? [],
    );
  } catch {
    return mergedItems;
  }
}

export const useCartStore = create<CartState>((set, get) => ({
  items: readItems(),
  loading: false,
  storageKey: currentStorageKey(),

  load: async () => {
    const subject = currentSubject();
    const storageKey = storageKeyForSubject(subject);
    const localItems = readItems(storageKey);
    set({ items: localItems, storageKey, loading: true });

    if (!subject) {
      set({ loading: false });
      return;
    }

    try {
      const items = await enqueueCartLoad(() =>
        loadAuthenticatedCart(subject),
      );
      if (!items) return;
      persist(storageKey, items);
      if (currentSubject() === subject) {
        set({ items, storageKey, loading: false });
      }
    } catch {
      if (currentSubject() === subject) set({ loading: false });
    }
  },

  add: async (product, quantity = 1) => {
    const subject = currentSubject();
    const storageKey = storageKeyForSubject(subject);
    const original =
      get().storageKey === storageKey ? get().items : readItems(storageKey);
    const available = Math.max(product.stock ?? quantity, 0);
    const existing = original.find(
      (item) => item.product.productId === product.productId,
    );
    const desired = Math.min((existing?.quantity ?? 0) + quantity, available);
    const quantityToAdd = desired - (existing?.quantity ?? 0);
    if (quantityToAdd <= 0) return;

    const items = existing
      ? original.map((item) =>
          item.product.productId === product.productId
            ? { ...item, quantity: desired }
            : item,
        )
      : [...original, { product, quantity: Math.min(quantity, available), price: product.price }];

    persist(storageKey, items);
    if (currentStorageKey() === storageKey) set({ items, storageKey });

    if (!subject) return;
    try {
      await api.post('/cart', {
        productId: product.productId,
        quantity: quantityToAdd,
      });
    } catch (error) {
      persist(storageKey, original);
      if (currentStorageKey() === storageKey) set({ items: original });
      throw error;
    }
  },

  updateQuantity: async (productId, quantity) => {
    const subject = currentSubject();
    const storageKey = storageKeyForSubject(subject);
    const original =
      get().storageKey === storageKey ? get().items : readItems(storageKey);
    if (quantity <= 0) {
      await get().remove(productId);
      return;
    }
    const items = original.map((item) => {
      if (item.product.productId !== productId) return item;
      return {
        ...item,
        quantity: Math.min(quantity, Math.max(item.product.stock, 1)),
      };
    });
    const updatedQuantity =
      items.find((item) => item.product.productId === productId)?.quantity ??
      quantity;
    persist(storageKey, items);
    if (currentStorageKey() === storageKey) set({ items, storageKey });

    if (!subject) return;
    try {
      await api.put('/cart', { productId, quantity: updatedQuantity });
    } catch (error) {
      persist(storageKey, original);
      if (currentStorageKey() === storageKey) set({ items: original });
      throw error;
    }
  },

  remove: async (productId) => {
    const subject = currentSubject();
    const storageKey = storageKeyForSubject(subject);
    const original =
      get().storageKey === storageKey ? get().items : readItems(storageKey);
    const items = original.filter(
      (item) => item.product.productId !== productId,
    );
    persist(storageKey, items);
    if (currentStorageKey() === storageKey) set({ items, storageKey });

    if (!subject) return;
    try {
      await api.delete(`/cart/${productId}`);
    } catch (error) {
      persist(storageKey, original);
      if (currentStorageKey() === storageKey) set({ items: original });
      throw error;
    }
  },

  clear: async () => {
    const subject = currentSubject();
    const storageKey = storageKeyForSubject(subject);
    const original =
      get().storageKey === storageKey ? get().items : readItems(storageKey);
    persist(storageKey, []);
    if (currentStorageKey() === storageKey) {
      set({ items: [], storageKey });
    }

    if (!subject) return;
    try {
      await api.delete('/cart/clear');
    } catch (error) {
      persist(storageKey, original);
      if (currentStorageKey() === storageKey) set({ items: original });
      throw error;
    }
  },
}));

export function cartItemCount(items: CartItem[]): number {
  return items.reduce((total, item) => total + item.quantity, 0);
}

export function cartTotal(items: CartItem[]): number {
  return items.reduce(
    (total, item) => total + item.price * item.quantity,
    0,
  );
}
