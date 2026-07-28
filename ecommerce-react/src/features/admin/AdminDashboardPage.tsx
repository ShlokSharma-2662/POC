import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { adminApi } from './admin-api';
import { PageHero } from './admin-components';
import styles from './Admin.module.scss';
import {
  formatCurrency,
  formatNumber,
  orderTotal,
  displayError,
} from './utils';

const managementLinks = [
  {
    to: '/admin/products',
    title: 'Manage products',
    description: 'Create products, update inventory, and manage catalog images.',
  },
  {
    to: '/admin/orders',
    title: 'Manage orders',
    description: 'Track fulfilment and move orders through valid status transitions.',
  },
  {
    to: '/admin/users',
    title: 'Manage users',
    description: 'Review accounts, assign roles, and manage account access.',
  },
  {
    to: '/admin/revenue',
    title: 'Revenue reports',
    description: 'Explore revenue trends and export period-level reports.',
  },
  {
    to: '/admin/performance',
    title: 'API performance',
    description: 'Inspect latency, throughput, and endpoint health.',
  },
  {
    to: '/admin/errors',
    title: 'Error logs',
    description: 'Search recent application failures and inspect diagnostic details.',
  },
] as const;

export function AdminDashboardPage() {
  const orders = useQuery({
    queryKey: ['admin', 'dashboard', 'orders'],
    queryFn: () => adminApi.getOrders({ page: 1, pageSize: 1000 }),
  });
  const users = useQuery({
    queryKey: ['admin', 'dashboard', 'users'],
    queryFn: () => adminApi.getUsers({ page: 1, pageSize: 1 }),
  });
  const errors = useQuery({
    queryKey: ['admin', 'dashboard', 'errors'],
    queryFn: () => adminApi.getErrors({ page: 1, pageSize: 1000 }),
  });

  const revenue =
    orders.data?.items.reduce((sum, order) => sum + orderTotal(order), 0) ?? 0;
  const since = errors.dataUpdatedAt - 24 * 60 * 60 * 1000;
  const errors24h =
    errors.data?.items.filter(
      (log) => new Date(log.timestamp).getTime() >= since,
    ).length ?? 0;
  const firstError = orders.error ?? users.error ?? errors.error;

  return (
    <main className={styles.page}>
      <PageHero
        title="Admin dashboard"
        description="Control the storefront, monitor reliability, and keep operations moving."
        actions={
          <>
            <Link className={styles.secondaryButton} to="/admin/orders">
              View orders
            </Link>
            <Link className={styles.secondaryButton} to="/admin/performance">
              Performance
            </Link>
          </>
        }
      />

      {firstError ? (
        <div className={`${styles.message} ${styles.errorMessage}`} role="alert">
          Some dashboard figures could not be loaded.{' '}
          {displayError(firstError, 'Refresh the page to try again.')}
        </div>
      ) : null}

      <section className={styles.statsGrid} aria-label="Store summary">
        <article className={styles.statCard}>
          <span className={styles.statLabel}>Total orders</span>
          <strong className={styles.statValue}>
            {orders.isPending ? '—' : formatNumber(orders.data?.totalCount ?? 0)}
          </strong>
        </article>
        <article className={styles.statCard}>
          <span className={styles.statLabel}>Revenue</span>
          <strong className={styles.statValue}>
            {orders.isPending ? '—' : formatCurrency(revenue)}
          </strong>
          {orders.data && orders.data.totalCount > orders.data.items.length ? (
            <small className={styles.statMeta}>
              Latest {formatNumber(orders.data.items.length)} orders
            </small>
          ) : null}
        </article>
        <article className={styles.statCard}>
          <span className={styles.statLabel}>Registered users</span>
          <strong className={styles.statValue}>
            {users.isPending ? '—' : formatNumber(users.data?.totalCount ?? 0)}
          </strong>
        </article>
        <article className={styles.statCard}>
          <span className={styles.statLabel}>Errors in 24 hours</span>
          <strong className={styles.statValue}>
            {errors.isPending ? '—' : formatNumber(errors24h)}
          </strong>
        </article>
      </section>

      <section aria-labelledby="admin-management-heading">
        <div className={styles.panelHeader}>
          <h2 id="admin-management-heading">Management and monitoring</h2>
        </div>
        <div className={styles.managementGrid}>
          {managementLinks.map((item) => (
            <Link className={styles.managementCard} to={item.to} key={item.to}>
              <h2>{item.title}</h2>
              <p>{item.description}</p>
              <span>Open →</span>
            </Link>
          ))}
        </div>
      </section>
    </main>
  );
}
