import { useEffect, useId, type ReactNode } from 'react';
import { createPortal } from 'react-dom';
import styles from './Modal.module.scss';

interface ModalProps {
  open: boolean;
  title: string;
  children: ReactNode;
  footer?: ReactNode;
  onClose: () => void;
  size?: 'sm' | 'md' | 'lg' | 'xl';
}

export function Modal({
  open,
  title,
  children,
  footer,
  onClose,
  size = 'md',
}: ModalProps) {
  const titleId = useId();

  useEffect(() => {
    if (!open) return;
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') onClose();
    };
    document.addEventListener('keydown', onKeyDown);
    return () => {
      document.body.style.overflow = previousOverflow;
      document.removeEventListener('keydown', onKeyDown);
    };
  }, [onClose, open]);

  if (!open) return null;

  return createPortal(
    <div className={styles.backdrop} onMouseDown={onClose}>
      <section
        className={`${styles.dialog} ${styles[size]}`}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        onMouseDown={(event) => event.stopPropagation()}
      >
        <header className={styles.header}>
          <h2 id={titleId}>{title}</h2>
          <button
            type="button"
            className={styles.close}
            onClick={onClose}
            aria-label="Close dialog"
          >
            ×
          </button>
        </header>
        <div className={styles.body}>{children}</div>
        {footer ? <footer className={styles.footer}>{footer}</footer> : null}
      </section>
    </div>,
    document.body,
  );
}
