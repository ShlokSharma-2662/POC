import { environment } from '../config/environment';
import type { ApiProblem, ApiResponse } from '../types/api';
import { getStoredToken } from './storage';

export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly details?: unknown,
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

interface RequestOptions {
  unwrap?: boolean;
  authenticated?: boolean;
}

function normalizeEnvelope<T>(value: unknown): ApiResponse<T> | null {
  if (!value || typeof value !== 'object') return null;
  const object = value as Record<string, unknown>;
  const success = object.isSuccessful ?? object.IsSuccessful;
  if (typeof success !== 'boolean') return null;

  return {
    isSuccessful: success,
    status: String(object.status ?? object.Status ?? ''),
    statusReason: String(object.statusReason ?? object.StatusReason ?? ''),
    data: (object.data ?? object.Data ?? null) as T | null,
  };
}

async function parseBody(response: Response): Promise<unknown> {
  if (response.status === 204) return null;
  const text = await response.text();
  if (!text) return null;

  try {
    return JSON.parse(text);
  } catch {
    return text;
  }
}

function errorMessage(body: unknown, fallback: string): string {
  if (typeof body === 'string' && body.trim()) return body;
  if (!body || typeof body !== 'object') return fallback;
  const problem = body as ApiProblem & Record<string, unknown>;
  return (
    problem.statusReason ||
    String(problem.StatusReason || '') ||
    problem.message ||
    problem.title ||
    fallback
  );
}

export async function request<T>(
  path: string,
  init: RequestInit = {},
  options: RequestOptions = {},
): Promise<T> {
  const url = path.startsWith('http')
    ? path
    : `${environment.apiBaseUrl}${path.startsWith('/') ? path : `/${path}`}`;
  const headers = new Headers(init.headers);
  const token = getStoredToken();

  if (!(init.body instanceof FormData) && init.body && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }

  if (options.authenticated !== false && token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  const response = await fetch(url, { ...init, headers });
  const body = await parseBody(response);

  if (!response.ok) {
    if (response.status === 401) {
      window.dispatchEvent(new CustomEvent('auth:unauthorized'));
    }
    if (response.status === 429) {
      window.dispatchEvent(
        new CustomEvent('api:rate-limit', {
          detail: {
            retryAfter: response.headers.get('Retry-After'),
            reset: response.headers.get('X-RateLimit-Reset'),
          },
        }),
      );
    }
    throw new ApiError(
      errorMessage(body, `Request failed with status ${response.status}`),
      response.status,
      body,
    );
  }

  if (options.unwrap === false) return body as T;
  const envelope = normalizeEnvelope<T>(body);
  if (!envelope) return body as T;
  if (!envelope.isSuccessful) {
    throw new ApiError(
      envelope.statusReason || 'API request failed',
      response.status,
      envelope,
    );
  }
  return envelope.data as T;
}

function jsonBody(value: unknown): string {
  return JSON.stringify(value);
}

export const api = {
  get: <T>(path: string, options?: RequestOptions) =>
    request<T>(path, { method: 'GET' }, options),
  post: <T>(path: string, body?: unknown, options?: RequestOptions) =>
    request<T>(
      path,
      { method: 'POST', body: body instanceof FormData ? body : jsonBody(body ?? {}) },
      options,
    ),
  put: <T>(path: string, body?: unknown, options?: RequestOptions) =>
    request<T>(
      path,
      { method: 'PUT', body: body instanceof FormData ? body : jsonBody(body ?? {}) },
      options,
    ),
  delete: <T>(path: string, body?: unknown, options?: RequestOptions) =>
    request<T>(
      path,
      {
        method: 'DELETE',
        body: body === undefined ? undefined : jsonBody(body),
      },
      options,
    ),
};
