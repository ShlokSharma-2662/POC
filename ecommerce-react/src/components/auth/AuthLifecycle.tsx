import { useEffect, useRef, type ReactNode } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { useLocation, useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import { getSubjectFromToken, isTokenExpired } from '../../lib/jwt';
import { isUserScopedQueryKey } from '../../lib/user-query';
import { useAuthStore } from '../../stores/auth-store';
import { useCartStore } from '../../stores/cart-store';
import { useWishlistStore } from '../../stores/wishlist-store';

export function AuthLifecycle({ children }: { children: ReactNode }) {
  const navigate = useNavigate();
  const location = useLocation();
  const queryClient = useQueryClient();
  const { token, logout } = useAuthStore();
  const subject = getSubjectFromToken(token);
  const previousSubject = useRef<string | null | undefined>(undefined);
  const loadCart = useCartStore((state) => state.load);
  const loadWishlist = useWishlistStore((state) => state.load);

  useEffect(() => {
    void loadCart();
    void loadWishlist();
  }, [loadCart, loadWishlist, token]);

  useEffect(() => {
    const identityChanged =
      previousSubject.current === undefined ||
      previousSubject.current !== subject;
    if (!identityChanged) return;

    queryClient.removeQueries({
      predicate: (query) =>
        isUserScopedQueryKey(query.queryKey) &&
        (subject === null || query.queryKey[1] !== subject),
    });
    previousSubject.current = subject;
  }, [queryClient, subject]);

  useEffect(() => {
    const handleUnauthorized = () => {
      logout();
      toast.error('Your session has expired. Please sign in again.');
      navigate('/login', {
        replace: true,
        state: { returnTo: location.pathname },
      });
    };
    const handleRateLimit = (event: Event) => {
      const detail = (event as CustomEvent).detail as
        | { retryAfter?: string | null }
        | undefined;
      const suffix = detail?.retryAfter
        ? ` Try again in ${detail.retryAfter} seconds.`
        : ' Please try again shortly.';
      toast.warning(`Too many requests.${suffix}`);
    };
    window.addEventListener('auth:unauthorized', handleUnauthorized);
    window.addEventListener('api:rate-limit', handleRateLimit);
    return () => {
      window.removeEventListener('auth:unauthorized', handleUnauthorized);
      window.removeEventListener('api:rate-limit', handleRateLimit);
    };
  }, [location.pathname, logout, navigate]);

  useEffect(() => {
    if (!token) return;
    const checkExpiry = () => {
      if (isTokenExpired(token)) {
        window.dispatchEvent(new CustomEvent('auth:unauthorized'));
      }
    };
    checkExpiry();
    const timer = window.setInterval(checkExpiry, 60_000);
    return () => window.clearInterval(timer);
  }, [token]);

  return children;
}
