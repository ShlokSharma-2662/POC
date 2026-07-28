import { create } from 'zustand';
import { api } from '../lib/api-client';
import { getSubjectFromToken } from '../lib/jwt';
import {
  getStoredToken,
  readJson,
  storageKeys,
  writeJson,
} from '../lib/storage';
import type { Product, WishlistItem } from '../types/domain';

interface GuestWishlistItem {
  product: Product;
  addedAt: string;
}

interface WishlistState {
  items: WishlistItem[];
  guestItems: GuestWishlistItem[];
  loading: boolean;
  load: () => Promise<void>;
  contains: (productId: number) => boolean;
  add: (product: Product) => Promise<void>;
  remove: (productId: number) => Promise<void>;
  clear: () => Promise<void>;
}

let wishlistLoadQueue: Promise<unknown> = Promise.resolve();

function enqueueWishlistLoad<T>(task: () => Promise<T>): Promise<T> {
  const result = wishlistLoadQueue.then(task, task);
  wishlistLoadQueue = result.then(
    () => undefined,
    () => undefined,
  );
  return result;
}

function currentSubject(): string | null {
  return getSubjectFromToken(getStoredToken());
}

function guestItems(): GuestWishlistItem[] {
  return readJson<GuestWishlistItem[]>(
    localStorage,
    storageKeys.guestWishlist,
    [],
  );
}

function persistGuest(items: GuestWishlistItem[]): void {
  writeJson(localStorage, storageKeys.guestWishlist, items);
}

function dedupeGuestItems(items: GuestWishlistItem[]): GuestWishlistItem[] {
  const deduped = new Map<number, GuestWishlistItem>();
  for (const item of items) {
    const productId = Number(item.product?.productId);
    if (!productId || deduped.has(productId)) continue;
    deduped.set(productId, item);
  }
  return [...deduped.values()];
}

function removeMergedGuestItem(productId: number): void {
  persistGuest(
    guestItems().filter((item) => item.product.productId !== productId),
  );
}

function wishlistItemFromGuest(item: GuestWishlistItem): WishlistItem {
  return {
    id: 0,
    productId: item.product.productId,
    productName: item.product.name,
    productDescription: item.product.description,
    productPrice: item.product.price,
    productImageUrl: item.product.imageUrl,
    categoryName: item.product.categoryName,
    stock: item.product.stock,
    addedAt: item.addedAt,
    isInStock: item.product.stock > 0,
  };
}

async function loadAuthenticatedWishlist(
  subject: string,
): Promise<WishlistItem[] | null> {
  if (currentSubject() !== subject) return null;

  const serverItems = (await api.get<WishlistItem[]>('/wishlist')) ?? [];
  if (currentSubject() !== subject) return null;

  const byProductId = new Map(
    serverItems.map((item) => [item.productId, item]),
  );
  const guests = dedupeGuestItems(guestItems());

  for (const guestItem of guests) {
    if (currentSubject() !== subject) break;

    const productId = guestItem.product.productId;
    if (byProductId.has(productId)) {
      removeMergedGuestItem(productId);
      continue;
    }

    try {
      await api.post('/wishlist/add', { productId, userId: 0 });
      removeMergedGuestItem(productId);
      byProductId.set(productId, wishlistItemFromGuest(guestItem));
    } catch {
      // Keep this guest item so a later authenticated load can retry it.
    }
  }

  if (currentSubject() !== subject) return null;

  try {
    return (await api.get<WishlistItem[]>('/wishlist')) ?? [];
  } catch {
    return [...byProductId.values()];
  }
}

export const useWishlistStore = create<WishlistState>((set, get) => ({
  items: [],
  guestItems: guestItems(),
  loading: false,

  load: async () => {
    const subject = currentSubject();
    if (!subject) {
      set({ guestItems: guestItems(), items: [], loading: false });
      return;
    }

    // Never leave a previous account's wishlist visible while the next one loads.
    set({ items: [], loading: true });
    try {
      const items = await enqueueWishlistLoad(() =>
        loadAuthenticatedWishlist(subject),
      );
      if (items && currentSubject() === subject) {
        set({ items, guestItems: guestItems(), loading: false });
      }
    } catch {
      if (currentSubject() === subject) set({ loading: false });
    }
  },

  contains: (productId) =>
    currentSubject()
      ? get().items.some((item) => item.productId === productId)
      : get().guestItems.some(
          (item) => item.product.productId === productId,
        ),

  add: async (product) => {
    const subject = currentSubject();
    if (!subject) {
      const current = get().guestItems;
      if (current.some((item) => item.product.productId === product.productId)) {
        return;
      }
      const guestItems = [
        ...current,
        { product, addedAt: new Date().toISOString() },
      ];
      persistGuest(guestItems);
      set({ guestItems });
      return;
    }

    await api.post('/wishlist/add', { productId: product.productId, userId: 0 });
    if (currentSubject() === subject) await get().load();
  },

  remove: async (productId) => {
    const subject = currentSubject();
    if (!subject) {
      const guestItems = get().guestItems.filter(
        (item) => item.product.productId !== productId,
      );
      persistGuest(guestItems);
      set({ guestItems });
      return;
    }

    await api.delete('/wishlist/remove', { productId, userId: 0 });
    if (currentSubject() === subject) {
      set({ items: get().items.filter((item) => item.productId !== productId) });
    }
  },

  clear: async () => {
    const subject = currentSubject();
    if (!subject) {
      persistGuest([]);
      set({ guestItems: [] });
      return;
    }
    await api.delete('/wishlist/clear');
    if (currentSubject() === subject) set({ items: [] });
  },
}));

export function wishlistCount(state: WishlistState): number {
  return currentSubject() ? state.items.length : state.guestItems.length;
}
