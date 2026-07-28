import { describe, expect, it } from 'vitest';
import {
  decodeJwt,
  getEmailFromToken,
  getRoleFromToken,
  getSubjectFromToken,
  isTokenExpired,
} from './jwt';

function tokenFor(payload: Record<string, unknown>): string {
  const encode = (value: unknown) =>
    btoa(JSON.stringify(value))
      .replace(/\+/g, '-')
      .replace(/\//g, '_')
      .replace(/=+$/, '');
  return `${encode({ alg: 'none', typ: 'JWT' })}.${encode(payload)}.signature`;
}

describe('JWT helpers', () => {
  it('reads the backend role and identity claims', () => {
    const token = tokenFor({
      sub: '42',
      email: 'user@example.com',
      'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': 'Admin',
      exp: Math.floor(Date.now() / 1000) + 300,
    });

    expect(decodeJwt(token)?.sub).toBe('42');
    expect(getSubjectFromToken(token)).toBe('42');
    expect(getEmailFromToken(token)).toBe('user@example.com');
    expect(getRoleFromToken(token)).toBe('Admin');
    expect(isTokenExpired(token)).toBe(false);
  });

  it('rejects malformed and expired tokens safely', () => {
    const expired = tokenFor({ exp: Math.floor(Date.now() / 1000) - 1 });

    expect(decodeJwt('not-a-token')).toBeNull();
    expect(getRoleFromToken(null)).toBeNull();
    expect(isTokenExpired(expired)).toBe(true);
  });
});
