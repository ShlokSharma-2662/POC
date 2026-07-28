import { useState } from 'react';
import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import { useAuthStore } from '../../stores/auth-store';
import { cartItemCount, useCartStore } from '../../stores/cart-store';
import {
  useWishlistStore,
  wishlistCount,
} from '../../stores/wishlist-store';
import { Modal } from '../ui/Modal';
import styles from './AppShell.module.scss';

export function AppShell() {
  const navigate = useNavigate();
  const [confirmingLogout, setConfirmingLogout] = useState(false);
  const auth = useAuthStore();
  const cartCount = useCartStore((state) => cartItemCount(state.items));
  const wishlist = useWishlistStore();
  const savedCount = wishlistCount(wishlist);

  const logout = () => {
    auth.logout();
    setConfirmingLogout(false);
    toast.success('You have been signed out.');
    navigate('/home');
  };

  return (
    <div className={styles.app}>
      <header className={styles.header}>
        <nav className={`${styles.nav} container`} aria-label="Main navigation">
          <Link className={styles.brand} to="/home" aria-label="ShopSphere home">
            <img src="/assets/images/logo.png" alt="" />
            <span>ShopSphere</span>
          </Link>
          <div className={styles.links}>
            <NavLink to="/home">Home</NavLink>
            <NavLink to="/products">Products</NavLink>
            <NavLink to="/wishlist">
              Wishlist <span className={styles.badge}>{savedCount}</span>
            </NavLink>
            <NavLink to="/cart">
              Cart <span className={styles.badge}>{cartCount}</span>
            </NavLink>
            {auth.isAuthenticated ? <NavLink to="/my-orders">Orders</NavLink> : null}
            {auth.role?.toLowerCase() === 'admin' ? (
              <NavLink to="/admin">Admin</NavLink>
            ) : null}
          </div>
          <div className={styles.actions}>
            {auth.isAuthenticated ? (
              <>
                <NavLink className="btn btn-outline-primary" to="/profile">
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
      </header>
      <main className={styles.main}>
        <Outlet />
      </main>
      <footer className={styles.footer}>
        <div className="container">
          <span>© {new Date().getFullYear()} ShopSphere</span>
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
