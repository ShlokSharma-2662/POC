import { create } from 'zustand';
import type { AuthUser } from '../types/domain';
import {
  getEmailFromToken,
  getRoleFromToken,
  isTokenExpired,
} from '../lib/jwt';
import {
  clearStoredAuthentication,
  getStoredToken,
  readJson,
  storageKeys,
  storeToken,
  writeJson,
} from '../lib/storage';

interface AuthState {
  token: string | null;
  user: AuthUser | null;
  role: string | null;
  isAuthenticated: boolean;
  setToken: (token: string, user?: AuthUser | null) => void;
  setOAuthUser: (user: AuthUser) => void;
  logout: () => void;
}

function initialAuthentication(): Pick<
  AuthState,
  'token' | 'user' | 'role' | 'isAuthenticated'
> {
  const storedToken = getStoredToken();
  const token = storedToken && !isTokenExpired(storedToken) ? storedToken : null;
  const oauthUser = readJson<AuthUser | null>(
    sessionStorage,
    storageKeys.oauthUser,
    null,
  );
  const role = getRoleFromToken(token) ?? oauthUser?.role ?? null;
  const email = getEmailFromToken(token);
  const user = oauthUser ?? (email ? { email, role: role ?? undefined } : null);

  if (!token && storedToken) clearStoredAuthentication();
  return {
    token,
    user,
    role,
    isAuthenticated: Boolean(token || oauthUser),
  };
}

export const useAuthStore = create<AuthState>((set) => ({
  ...initialAuthentication(),
  setToken: (token, user = null) => {
    storeToken(token);
    const role = getRoleFromToken(token) ?? user?.role ?? null;
    set({
      token,
      role,
      user:
        user ??
        ({
          email: getEmailFromToken(token) ?? '',
          role: role ?? undefined,
        } satisfies AuthUser),
      isAuthenticated: true,
    });
  },
  setOAuthUser: (user) => {
    writeJson(sessionStorage, storageKeys.oauthUser, user);
    sessionStorage.setItem(storageKeys.oauthAuthenticated, 'true');
    set((state) => ({
      user,
      role: state.role ?? user.role ?? null,
      isAuthenticated: true,
    }));
  },
  logout: () => {
    clearStoredAuthentication();
    set({ token: null, user: null, role: null, isAuthenticated: false });
  },
}));
