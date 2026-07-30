import { useState, type FormEvent } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import { api } from '../../lib/api-client';
import { useAuthStore } from '../../stores/auth-store';
import { cartItemCount, useCartStore } from '../../stores/cart-store';
import {
  useWishlistStore,
  wishlistCount,
} from '../../stores/wishlist-store';
import type { StorefrontCategory } from '../../features/storefront/storefront-utils';
import { Modal } from '../ui/Modal';
import styles from './AppShell.module.scss';

export function AppShell() {
  const navigate = useNavigate();
  const [searchInput, setSearchInput] = useState('');
  const [confirmingLogout, setConfirmingLogout] = useState(false);
  const auth = useAuthStore();
  const cartCount = useCartStore((state) => cartItemCount(state.items));
  const wishlist = useWishlistStore();
  const savedCount = wishlistCount(wishlist);
  const quickLinks = [
    ['Home', '/home'],
    ['Products', '/products'],
    ['Orders', '/my-orders'],
    ['My account', '/profile'],
  ] as const;
  const categoriesQuery = useQuery({
    queryKey: ['storefront', 'categories'],
    queryFn: () =>
      api.get<StorefrontCategory[]>('/categories', {
        authenticated: false,
      }),
  });
  const searchHintNames = categoriesQuery.data
    ? categoriesQuery.data.map((category) => category.name)
    : [];

  const logout = () => {
    auth.logout();
    setConfirmingLogout(false);
    toast.success('You have been signed out.');
    navigate('/home');
  };

  const submitSearch = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const query = searchInput.trim();
    const next = new URLSearchParams();
    if (query.length > 0) {
      next.set('search', query);
    }
    const target = `/products${next.toString() ? `?${next}` : ''}`;
    navigate(target);
  };

  return (
    <div className={styles.app}>
      <header className={styles.header}>
        <nav className={`${styles.nav} container`} aria-label="Main navigation">
          <Link className={styles.brand} to="/home" aria-label="ShopSphere home">
            <img src="/assets/images/logo.png" alt="" />
            <span>ShopSphere</span>
          </Link>
          <form
            className={styles.searchForm}
            onSubmit={submitSearch}
            role="search"
          >
            <input
              value={searchInput}
              type="search"
              placeholder="Search catalog"
              autoComplete="off"
              list="app-header-suggestions"
              onChange={(event) => setSearchInput(event.target.value)}
              aria-label="Search products"
            />
            <button type="submit" aria-label="Search for products">
              Search
            </button>
          </form>
          <div className={styles.links}>
            {quickLinks.map(([label, path]) => (
              <NavLink className={styles.navLink} key={path} to={path}>
                {label}
              </NavLink>
            ))}
            {auth.role?.toLowerCase() === 'admin' ? (
              <NavLink className={styles.navLink} to="/admin">
                Admin
              </NavLink>
            ) : null}
          </div>
          <div className={styles.actions}>
            <NavLink className={styles.navPill} to="/wishlist">
              Wishlist
              <span className={styles.badge}>{savedCount}</span>
            </NavLink>
            <NavLink className={styles.navPill} to="/cart">
              Cart
              <span className={styles.badge}>{cartCount}</span>
            </NavLink>
            {auth.isAuthenticated ? (
              <>
                <NavLink className={styles.navPill} to="/profile">
                  Profile
                </NavLink>
                <button
                  className="btn btn-primary"
                  type="button"
                  onClick={() => setConfirmingLogout(true)}
                >
                  Sign out
                </button>
              </>
            ) : (
              <>
                <NavLink className="btn btn-outline-primary" to="/login">
                  Sign in
                </NavLink>
                <NavLink className="btn btn-primary" to="/register">
                  Create account
                </NavLink>
              </>
            )}
          </div>
        </nav>
        <nav className={`${styles.secondaryNav} container`} aria-label="Browse categories">
          <div className={styles.categoryStrip}>
            <span className={styles.categoryLabel}>Shop by:</span>
            <Link to="/products">All</Link>
            {(categoriesQuery.data ?? []).slice(0, 5).map((category) => (
              <button
                className={styles.secondaryLink}
                type="button"
                key={category.id}
                onClick={() => navigate(`/products?category=${category.id}`)}
              >
                {category.name}
              </button>
            ))}
            <Link to="/products?inStockOnly=true">Only in stock</Link>
            <a href="mailto:support@eshop.com">Support</a>
          </div>
        </nav>
      </header>
      {searchHintNames.length > 0 ? (
        <datalist id="app-header-suggestions">
          {searchHintNames.map((name) => (
            <option key={name} value={name} />
          ))}
        </datalist>
      ) : null}
      <main className={styles.main}>
        <Outlet />
      </main>
      <nav className={styles.mobileBottomBar} aria-label="Mobile quick actions">
        <NavLink className={styles.mobileAction} to="/home">
          Home
        </NavLink>
        <NavLink className={styles.mobileAction} to="/products">
          Catalog
        </NavLink>
        <NavLink className={styles.mobileAction} to="/wishlist">
          Wishlist
          <span className={styles.mobileBadge}>{savedCount}</span>
        </NavLink>
        <NavLink className={styles.mobileAction} to="/cart">
          Cart
          <span className={styles.mobileBadge}>{cartCount}</span>
        </NavLink>
        {auth.isAuthenticated ? (
          <NavLink className={styles.mobileAction} to="/profile">
            Account
          </NavLink>
        ) : (
          <NavLink className={styles.mobileAction} to="/login">
            Sign in
          </NavLink>
        )}
      </nav>
      <footer className={styles.footer}>
        <div className="container">
          <span>Â© {new Date().getFullYear()} ShopSphere</span>
          <span>Secure shopping, powered by the Ecommerce API</span>
        </div>
      </footer>
      <Modal
        open={confirmingLogout}
        title="Sign out?"
        size="sm"
        onClose={() => setConfirmingLogout(false)}
        footer={
          <>
            <button
              className="btn btn-outline-secondary"
              type="button"
              onClick={() => setConfirmingLogout(false)}
            >
              Cancel
            </button>
            <button className="btn btn-primary" type="button" onClick={logout}>
              Sign out
            </button>
          </>
        }
      >
        Your cart is saved, and you can sign back in at any time.
      </Modal>
    </div>
  );
}
