import { Link, NavLink, Outlet } from 'react-router-dom';
import styles from './AdminShell.module.scss';

const adminLinks = [
  ['/admin', 'Overview'],
  ['/admin/orders', 'Orders'],
  ['/admin/users', 'Users'],
  ['/admin/products', 'Products'],
  ['/admin/performance', 'Performance'],
  ['/admin/errors', 'Error logs'],
  ['/admin/revenue', 'Revenue'],
] as const;

export function AdminShell() {
  return (
    <div className={styles.layout}>
      <aside className={styles.sidebar}>
        <Link className={styles.title} to="/admin">
          <span>Admin console</span>
        </Link>
        <nav aria-label="Administration">
          {adminLinks.map(([to, label]) => (
            <NavLink key={to} to={to} end={to === '/admin'}>
              {label}
            </NavLink>
          ))}
        </nav>
        <Link className={styles.back} to="/home">
          ← Return to store
        </Link>
      </aside>
      <section className={styles.content}>
        <Outlet />
      </section>
    </div>
  );
}
