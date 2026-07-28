export interface JwtPayload {
  sub?: string;
  exp?: number;
  email?: string;
  name?: string;
  role?: string | string[];
  roles?: string[];
  [key: string]: unknown;
}

const roleClaim =
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
const emailClaim =
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress';

function decodeBase64Url(value: string): string {
  const normalized = value.replace(/-/g, '+').replace(/_/g, '/');
  const padded = normalized.padEnd(
    normalized.length + ((4 - (normalized.length % 4)) % 4),
    '=',
  );
  return decodeURIComponent(
    atob(padded)
      .split('')
      .map((character) => {
        return `%${character.charCodeAt(0).toString(16).padStart(2, '0')}`;
      })
      .join(''),
  );
}

export function decodeJwt(token: string | null): JwtPayload | null {
  if (!token) return null;

  try {
    const payload = token.split('.')[1];
    return payload ? (JSON.parse(decodeBase64Url(payload)) as JwtPayload) : null;
  } catch {
    return null;
  }
}

export function getRoleFromToken(token: string | null): string | null {
  const payload = decodeJwt(token);
  if (!payload) return null;
  const rawRole = payload[roleClaim] ?? payload.role ?? payload.roles;
  return Array.isArray(rawRole) ? String(rawRole[0] ?? '') || null : String(rawRole || '') || null;
}

export function getEmailFromToken(token: string | null): string | null {
  const payload = decodeJwt(token);
  const value = payload?.email ?? payload?.[emailClaim];
  return typeof value === 'string' ? value : null;
}

export function getSubjectFromToken(token: string | null): string | null {
  return decodeJwt(token)?.sub ?? null;
}

export function isTokenExpired(token: string | null): boolean {
  const expiry = decodeJwt(token)?.exp;
  return typeof expiry === 'number' && expiry * 1000 <= Date.now();
}
