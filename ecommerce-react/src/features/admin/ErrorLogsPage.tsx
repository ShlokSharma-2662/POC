import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { toast } from 'sonner';
import { Modal } from '../../components/ui/Modal';
import { Pagination } from '../../components/ui/Pagination';
import { adminApi } from './admin-api';
import {
  EmptyState,
  ErrorState,
  PageHero,
  StatusBadge,
  TableLoader,
} from './admin-components';
import styles from './Admin.module.scss';
import type { ErrorLog } from './types';
import { useDebouncedValue } from './use-debounced-value';
import {
  datedFilename,
  displayError,
  downloadCsv,
  formatDate,
  formatNumber,
  totalPages,
} from './utils';

export function ErrorLogsPage() {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [search, setSearch] = useState('');
  const [severity, setSeverity] = useState('');
  const [selectedLog, setSelectedLog] = useState<ErrorLog | null>(null);
  const debouncedSearch = useDebouncedValue(search.trim());

  const errorsQuery = useQuery({
    queryKey: [
      'admin',
      'errors',
      page,
      pageSize,
      debouncedSearch,
      severity,
    ],
    queryFn: () =>
      adminApi.getErrors({
        page,
        pageSize,
        search: debouncedSearch || undefined,
        severity: severity || undefined,
      }),
    placeholderData: keepPreviousData,
  });

  const logs = errorsQuery.data?.items ?? [];
  const count = errorsQuery.data?.totalCount ?? 0;

  function clearFilters() {
    setSearch('');
    setSeverity('');
    setPage(1);
  }

  function exportLogs() {
    if (!logs.length) return;
    downloadCsv(datedFilename('error_logs_export'), logs, [
      { label: 'ID', value: (log) => log.id },
      { label: 'Timestamp', value: (log) => log.timestamp },
      { label: 'Severity', value: (log) => log.severity },
      { label: 'Path', value: (log) => log.path },
      { label: 'Message', value: (log) => log.message },
      { label: 'IP address', value: (log) => log.ipAddress ?? '' },
      { label: 'User agent', value: (log) => log.userAgent ?? '' },
      { label: 'Stack trace', value: (log) => log.stackTrace ?? '' },
    ]);
    toast.success(`Exported ${logs.length} error logs.`);
  }

  return (
    <main className={styles.page}>
      <PageHero
        title="Error logs"
        description="Search application failures and inspect the context needed for diagnosis."
        actions={
          <button
            className={styles.secondaryButton}
            type="button"
            onClick={exportLogs}
            disabled={!logs.length}
          >
            Export current page
          </button>
        }
      />

      <section className={styles.panel} aria-label="Error log filters">
        <div className={styles.filters}>
          <div className={styles.filterField}>
            <label htmlFor="errors-search">Search logs</label>
            <input
              className={styles.input}
              id="errors-search"
              type="search"
              value={search}
              placeholder="Message, path, user agent, or IP address"
              onChange={(event) => {
                setSearch(event.target.value);
                setPage(1);
              }}
            />
          </div>
          <div className={styles.filterField}>
            <label htmlFor="errors-severity">Severity</label>
            <select
              className={styles.select}
              id="errors-severity"
              value={severity}
              onChange={(event) => {
                setSeverity(event.target.value);
                setPage(1);
              }}
            >
              <option value="">All severities</option>
              <option value="Critical">Critical</option>
              <option value="Error">Error</option>
              <option value="Warning">Warning</option>
              <option value="Info">Info</option>
            </select>
          </div>
          <div className={styles.filterField}>
            <label htmlFor="errors-page-size">Rows per page</label>
            <select
              className={styles.select}
              id="errors-page-size"
              value={pageSize}
              onChange={(event) => {
                setPageSize(Number(event.target.value));
                setPage(1);
              }}
            >
              {[10, 25, 50].map((size) => (
                <option key={size} value={size}>
                  {size}
                </option>
              ))}
            </select>
          </div>
          <div className={styles.filterActions}>
            <button
              className={styles.ghostButton}
              type="button"
              onClick={clearFilters}
              disabled={!search && !severity}
            >
              Clear
            </button>
            <button
              className={styles.secondaryButton}
              type="button"
              onClick={() => void errorsQuery.refetch()}
              disabled={errorsQuery.isFetching}
            >
              Refresh
            </button>
          </div>
        </div>
      </section>

      <section className={`${styles.panel} ${styles.tablePanel}`}>
        {errorsQuery.isPending ? (
          <TableLoader label="Loading error logs…" />
        ) : errorsQuery.isError ? (
          <ErrorState
            message={displayError(
              errorsQuery.error,
              'Error logs could not be loaded.',
            )}
            onRetry={() => void errorsQuery.refetch()}
          />
        ) : logs.length === 0 ? (
          <EmptyState
            title="No error logs found"
            description="No records match the current search and severity filters."
          />
        ) : (
          <>
            <div className={styles.tableScroll}>
              <table className={styles.table}>
                <caption className={styles.srOnly}>
                  Application error and alert logs
                </caption>
                <thead>
                  <tr>
                    <th scope="col">Time</th>
                    <th scope="col">Severity</th>
                    <th scope="col">Path</th>
                    <th scope="col">Message</th>
                    <th scope="col">IP address</th>
                    <th className={styles.actionsCell} scope="col">
                      Actions
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {logs.map((log) => (
                    <tr key={log.id}>
                      <td>{formatDate(log.timestamp)}</td>
                      <td>
                        <StatusBadge value={log.severity} />
                      </td>
                      <td>
                        <span
                          className={`${styles.monospace} ${styles.truncate}`}
                          title={log.path}
                        >
                          {log.path}
                        </span>
                      </td>
                      <td>
                        <span className={styles.truncate} title={log.message}>
                          {log.message}
                        </span>
                      </td>
                      <td>{log.ipAddress || '—'}</td>
                      <td className={styles.actionsCell}>
                        <button
                          className={`${styles.secondaryButton} ${styles.compactButton}`}
                          type="button"
                          onClick={() => setSelectedLog(log)}
                        >
                          Details
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <div className={styles.tableFooter}>
              <span>
                Showing {(page - 1) * pageSize + 1}–
                {Math.min(page * pageSize, count)} of {formatNumber(count)}
              </span>
              <Pagination
                page={page}
                totalPages={totalPages(count, pageSize)}
                onPageChange={setPage}
              />
            </div>
          </>
        )}
      </section>

      <Modal
        open={selectedLog !== null}
        title={selectedLog ? `Error log ${selectedLog.id}` : 'Error details'}
        size="lg"
        onClose={() => setSelectedLog(null)}
        footer={
          <button
            className={styles.ghostButton}
            type="button"
            onClick={() => setSelectedLog(null)}
          >
            Close
          </button>
        }
      >
        {selectedLog ? (
          <>
            <dl className={styles.detailsGrid}>
              <div className={styles.detail}>
                <dt>Severity</dt>
                <dd>
                  <StatusBadge value={selectedLog.severity} />
                </dd>
              </div>
              <div className={styles.detail}>
                <dt>Timestamp</dt>
                <dd>{formatDate(selectedLog.timestamp)}</dd>
              </div>
              <div className={`${styles.detail} ${styles.wideField}`}>
                <dt>Path</dt>
                <dd className={styles.monospace}>{selectedLog.path}</dd>
              </div>
              <div className={styles.detail}>
                <dt>IP address</dt>
                <dd>{selectedLog.ipAddress || 'Not recorded'}</dd>
              </div>
              <div className={styles.detail}>
                <dt>User agent</dt>
                <dd>{selectedLog.userAgent || 'Not recorded'}</dd>
              </div>
              <div className={`${styles.detail} ${styles.wideField}`}>
                <dt>Message</dt>
                <dd>{selectedLog.message}</dd>
              </div>
            </dl>
            <h3>Stack trace</h3>
            <pre className={styles.stackTrace}>
              {selectedLog.stackTrace || 'No stack trace was recorded.'}
            </pre>
          </>
        ) : null}
      </Modal>
    </main>
  );
}
