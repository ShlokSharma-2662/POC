import type { QueryKey } from '@tanstack/react-query';

const userQueryScope = 'user';

export const userQueryKeys = {
  all: [userQueryScope] as const,
  orders: (subject: string) =>
    [userQueryScope, subject, 'storefront', 'my-orders'] as const,
  profile: (subject: string) =>
    [userQueryScope, subject, 'storefront', 'profile'] as const,
};

export function isUserScopedQueryKey(queryKey: QueryKey): boolean {
  return queryKey[0] === userQueryScope;
}
