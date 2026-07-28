export const storageKeys = {
  authToken: 'authToken',
  accessToken: 'accessToken',
  oauthUser: 'oauth_user',
  oauthAuthenticated: 'oauth_authenticated',
  guestCart: 'cart_guest',
  guestWishlist: 'guest_wishlist',
  lastOrderSummary: 'lastOrderSummary',
  lastOrderItems: 'lastOrderItems',
} as const;

export function getStoredToken(): string | null {
  return (
    localStorage.getItem(storageKeys.authToken) ||
    localStorage.getItem(storageKeys.accessToken)
  );
}

export function storeToken(token: string): void {
  localStorage.setItem(storageKeys.authToken, token);
  localStorage.setItem(storageKeys.accessToken, token);
}

export function clearStoredAuthentication(): void {
  localStorage.removeItem(storageKeys.authToken);
  localStorage.removeItem(storageKeys.accessToken);
  sessionStorage.removeItem(storageKeys.oauthUser);
  sessionStorage.removeItem(storageKeys.oauthAuthenticated);
}

export function readJson<T>(storage: Storage, key: string, fallback: T): T {
  const rawValue = storage.getItem(key);
  if (!rawValue) return fallback;

  try {
    return JSON.parse(rawValue) as T;
  } catch {
    return fallback;
  }
}

export function writeJson<T>(storage: Storage, key: string, value: T): void {
  storage.setItem(key, JSON.stringify(value));
}
