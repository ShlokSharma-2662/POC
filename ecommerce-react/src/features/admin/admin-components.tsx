import type { ReactNode } from 'react';
import { Loader } from '../../components/ui/Loader';
import styles from './Admin.module.scss';

interface PageHeroProps {
  title: string;
  description: string;
  actions?: ReactNode;
}

export function PageHero({ title, description, actions }: PageHeroProps) {
  return (
    <header className={styles.hero}>
      <div>
        <h1>{title}</h1>
        <p>{description}</p>
      </div>
      {actions ? <div className={styles.heroActions}>{actions}</div> : null}
    </header>
  );
}

interface ErrorStateProps {
  message: string;
  onRetry?: () => void;
}

export function ErrorState({ message, onRetry }: ErrorStateProps) {
  return (
    <div className={styles.errorState} role="alert">
      <h2>Unable to load data</h2>
      <p>{message}</p>
      {onRetry ? (
        <button className={styles.primaryButton} type="button" onClick={onRetry}>
          Try again
        </button>
      ) : null}
    </div>
  );
}

interface EmptyStateProps {
  title: string;
  description: string;
  action?: ReactNode;
}

export function EmptyState({ title, description, action }: EmptyStateProps) {
  return (
    <div className={styles.empty}>
      <h2>{title}</h2>
      <p>{description}</p>
      {action}
    </div>
  );
}

export function TableLoader({ label }: { label: string }) {
  return <Loader label={label} />;
}

function badgeTone(value: string): string {
  const normalized = value.toLowerCase();
  if (
    ['active', 'delivered', 'healthy', 'success', 'admin', '200', '201'].includes(
      normalized,
    )
  ) {
    return styles.success ?? '';
  }
  if (
    ['pending', 'confirmed', 'warning'].includes(normalized) ||
    normalized.startsWith('4')
  ) {
    return styles.warning ?? '';
  }
  if (
    ['cancelled', 'deactivated', 'critical', 'error', 'deleted'].includes(
      normalized,
    ) ||
    normalized.startsWith('5')
  ) {
    return styles.danger ?? '';
  }
  if (['shipped', 'info', 'user'].includes(normalized) || normalized.startsWith('3')) {
    return styles.info ?? '';
  }
  return styles.neutral ?? '';
}

export function StatusBadge({ value }: { value: string }) {
  return (
    <span className={`${styles.badge} ${badgeTone(value)}`}>{value}</span>
  );
}
