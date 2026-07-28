import { api } from '../lib/api-client';
import type { AuthResponse, AuthUser, UserProfile } from '../types/domain';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}

export interface ChangePasswordRequest {
  oldPassword: string;
  newPassword: string;
  confirmPassword: string;
}

export type OAuthProvider = 'microsoft' | 'google';

export interface OAuthExchangeResponse extends AuthResponse {
  name: string;
  picture?: string;
  returnTo?: string;
}

function oauthLoginUrl(
  path: string,
  returnUrl: string,
  returnTo?: string | null,
) {
  const params = new URLSearchParams({ returnUrl });
  if (returnTo) params.set('returnTo', returnTo);
  return `${path}?${params.toString()}`;
}

export const authService = {
  login: (payload: LoginRequest) =>
    api.post<AuthResponse>('/auth/login', payload, { authenticated: false }),
  register: (payload: RegisterRequest) =>
    api.post<AuthResponse>('/auth/register', payload, {
      authenticated: false,
    }),
  profile: () => api.get<UserProfile>('/auth/profile'),
  changePassword: (payload: ChangePasswordRequest) =>
    api.post<void>('/auth/change-password', payload),
  microsoftLoginUrl: (returnUrl: string, returnTo?: string | null) =>
    oauthLoginUrl('/api/oauth/login', returnUrl, returnTo),
  googleLoginUrl: (returnUrl: string, returnTo?: string | null) =>
    oauthLoginUrl('/api/google-oauth/login', returnUrl, returnTo),
  exchangeOAuthCode: (provider: OAuthProvider, code: string) =>
    api.post<OAuthExchangeResponse>(
      provider === 'microsoft'
        ? '/oauth/exchange'
        : '/google-oauth/exchange',
      { code },
      { authenticated: false },
    ),
  microsoftUserInfo: () => api.get<AuthUser>('/oauth/userinfo'),
  googleUserInfo: () => api.get<AuthUser>('/google-oauth/userinfo'),
};
