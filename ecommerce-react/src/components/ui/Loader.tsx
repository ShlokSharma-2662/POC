import styles from './Loader.module.scss';

interface LoaderProps {
  label?: string;
  fullPage?: boolean;
}

export function Loader({ label = 'Loading…', fullPage = false }: LoaderProps) {
  return (
    <div
      className={`${styles.loader} ${fullPage ? styles.fullPage : ''}`}
      role="status"
      aria-live="polite"
    >
      <span className={styles.spinner} aria-hidden="true" />
      <span>{label}</span>
    </div>
  );
}
