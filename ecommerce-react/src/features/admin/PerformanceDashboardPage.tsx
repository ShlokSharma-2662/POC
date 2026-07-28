import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query';
import type { ChartData, ChartOptions } from 'chart.js';
import { useEffect, useMemo, useState } from 'react';
import { Bar, Line } from 'react-chartjs-2';
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
import './charts';
import type { EndpointPerformance, SystemMetric } from './types';
import { useDebouncedValue } from './use-debounced-value';
import {
  aggregateEndpoints,
  datedFilename,
  displayError,
  downloadCsv,
  formatDate,
  formatNumber,
  metricsFromRange,
  totalPages,
} from './utils';

const lineOptions: ChartOptions<'line'> = {
  responsive: true,
  maintainAspectRatio: false,
  interaction: { intersect: false, mode: 'index' },
  plugins: {
    legend: { position: 'top' },
  },
  scales: {
    y: {
      beginAtZero: true,
      title: { display: true, text: 'Milliseconds' },
    },
  },
};

const barOptions: ChartOptions<'bar'> = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: { position: 'top' },
  },
  scales: {
    y: {
      beginAtZero: true,
      title: { display: true, text: 'Milliseconds' },
    },
  },
};

const errorOptions: ChartOptions<'line'> = {
  responsive: true,
  maintainAspectRatio: false,
  interaction: { intersect: false, mode: 'index' },
  plugins: {
    legend: { position: 'top' },
  },
  scales: {
    y: {
      beginAtZero: true,
      max: 100,
      title: { display: true, text: 'Error rate (%)' },
    },
  },
};

const noMetrics: SystemMetric[] = [];

export function PerformanceDashboardPage() {
  const queryClient = useQueryClient();
  const [timeRange, setTimeRange] = useState('24h');
  const [refreshSeconds, setRefreshSeconds] = useState(0);
  const [countdown, setCountdown] = useState(0);
  const [endpoint, setEndpoint] = useState('');
  const [statusCode, setStatusCode] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [chartType, setChartType] = useState<'line' | 'bar'>('line');
  const [selectedEndpoint, setSelectedEndpoint] =
    useState<EndpointPerformance | null>(null);
  const debouncedEndpoint = useDebouncedValue(endpoint.trim());

  function metricRequest(requestPage: number, requestPageSize: number) {
    return adminApi.getMetrics({
      page: requestPage,
      pageSize: requestPageSize,
      endpoint: debouncedEndpoint || undefined,
      statusCode: statusCode ? Number(statusCode) : undefined,
      fromDate: metricsFromRange(timeRange),
      toDate: new Date().toISOString(),
    });
  }

  const summaryQuery = useQuery({
    queryKey: [
      'admin',
      'metrics',
      'summary',
      timeRange,
      debouncedEndpoint,
      statusCode,
    ],
    queryFn: () => metricRequest(1, 1000),
  });

  const pageQuery = useQuery({
    queryKey: [
      'admin',
      'metrics',
      'page',
      timeRange,
      debouncedEndpoint,
      statusCode,
      page,
      pageSize,
    ],
    queryFn: () => metricRequest(page, pageSize),
    placeholderData: keepPreviousData,
  });

  const endpointQuery = useQuery({
    queryKey: ['admin', 'metrics', 'endpoint', selectedEndpoint?.name],
    queryFn: () =>
      adminApi.getMetrics({
        page: 1,
        pageSize: 50,
        endpoint: selectedEndpoint?.name,
        fromDate: metricsFromRange(timeRange),
        toDate: new Date().toISOString(),
      }),
    enabled: selectedEndpoint !== null,
  });

  const { refetch: refetchSummary } = summaryQuery;
  const { refetch: refetchPage } = pageQuery;

  useEffect(() => {
    if (refreshSeconds <= 0) return;
    const refreshTimer = window.setInterval(() => {
      void refetchSummary();
      void refetchPage();
      setCountdown(refreshSeconds);
    }, refreshSeconds * 1000);
    const countdownTimer = window.setInterval(() => {
      setCountdown((current) =>
        current <= 1 ? refreshSeconds : current - 1,
      );
    }, 1000);

    return () => {
      window.clearInterval(refreshTimer);
      window.clearInterval(countdownTimer);
    };
  }, [refreshSeconds, refetchPage, refetchSummary]);

  const seedMutation = useMutation({
    mutationFn: adminApi.seedMetrics,
    onSuccess: async (result) => {
      toast.success(
        `Seeded ${formatNumber(result?.count ?? 10)} sample metrics.`,
      );
      await queryClient.invalidateQueries({
        queryKey: ['admin', 'metrics'],
      });
    },
    onError: (error) =>
      toast.error(displayError(error, 'Sample metrics could not be seeded.')),
  });

  const metrics = summaryQuery.data?.items ?? noMetrics;
  const requests = pageQuery.data?.items ?? [];
  const count = pageQuery.data?.totalCount ?? 0;
  const endpoints = useMemo(() => aggregateEndpoints(metrics), [metrics]);
  const sortedMetrics = useMemo(
    () =>
      [...metrics].sort(
        (left, right) =>
          new Date(left.timestamp).getTime() -
          new Date(right.timestamp).getTime(),
      ),
    [metrics],
  );

  const averageResponseTime = metrics.length
    ? metrics.reduce((sum, metric) => sum + metric.responseTimeMs, 0) /
      metrics.length
    : 0;
  const errorCount = metrics.filter((metric) => metric.statusCode >= 400).length;
  const errorRate = metrics.length ? (errorCount / metrics.length) * 100 : 0;
  const successRate = metrics.length ? 100 - errorRate : 0;
  const timestamps = metrics
    .map((metric) => new Date(metric.timestamp).getTime())
    .filter(Number.isFinite);
  const sampleMinutes =
    timestamps.length > 1
      ? Math.max(
          1,
          (Math.max(...timestamps) - Math.min(...timestamps)) / 60_000,
        )
      : 1;
  const throughput = metrics.length / sampleMinutes;

  const labels = sortedMetrics.map((metric) =>
    new Intl.DateTimeFormat('en-IN', {
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
    }).format(new Date(metric.timestamp)),
  );
  const responseLineData: ChartData<'line'> = {
    labels,
    datasets: [
      {
        label: 'API response time',
        data: sortedMetrics.map((metric) => metric.responseTimeMs),
        borderColor: '#4f46e5',
        backgroundColor: 'rgb(79 70 229 / 12%)',
        pointRadius: sortedMetrics.length > 80 ? 0 : 2,
        tension: 0.3,
        fill: true,
      },
    ],
  };
  const responseBarData: ChartData<'bar'> = {
    labels,
    datasets: [
      {
        label: 'API response time',
        data: sortedMetrics.map((metric) => metric.responseTimeMs),
        borderColor: '#4f46e5',
        backgroundColor: 'rgb(79 70 229 / 70%)',
        borderWidth: 1,
      },
    ],
  };
  const errorData = buildErrorChart(sortedMetrics);
  const lastUpdated = Math.max(
    summaryQuery.dataUpdatedAt,
    pageQuery.dataUpdatedAt,
  );

  function clearFilters() {
    setEndpoint('');
    setStatusCode('');
    setPage(1);
  }

  function exportMetrics() {
    if (!requests.length) return;
    downloadCsv(datedFilename('performance_metrics_export'), requests, [
      { label: 'ID', value: (metric) => metric.id },
      { label: 'Timestamp', value: (metric) => metric.timestamp },
      { label: 'Method', value: (metric) => metric.method },
      { label: 'Endpoint', value: (metric) => metric.endpoint },
      { label: 'Status code', value: (metric) => metric.statusCode },
      {
        label: 'Response time (ms)',
        value: (metric) => metric.responseTimeMs,
      },
      {
        label: 'Threshold exceeded',
        value: (metric) => Boolean(metric.isThresholdExceeded),
      },
      { label: 'IP address', value: (metric) => metric.ipAddress ?? '' },
      { label: 'User agent', value: (metric) => metric.userAgent ?? '' },
    ]);
    toast.success(`Exported ${requests.length} metrics.`);
  }

  const firstError = summaryQuery.error ?? pageQuery.error;

  return (
    <main className={styles.page}>
      <PageHero
        title="Performance dashboard"
        description="Monitor API latency, request health, and endpoint-level behaviour."
        actions={
          <>
            <button
              className={styles.secondaryButton}
              type="button"
              onClick={exportMetrics}
              disabled={!requests.length}
            >
              Export current page
            </button>
            <button
              className={styles.secondaryButton}
              type="button"
              disabled={
                summaryQuery.isFetching ||
                pageQuery.isFetching ||
                seedMutation.isPending
              }
              onClick={() => seedMutation.mutate()}
            >
              Seed sample data
            </button>
          </>
        }
      />

      <section className={styles.panel} aria-label="Performance controls">
        <div className={styles.filters}>
          <div className={styles.filterField}>
            <label htmlFor="metrics-endpoint">Endpoint</label>
            <input
              className={styles.input}
              id="metrics-endpoint"
              type="search"
              value={endpoint}
              placeholder="/api/products"
              onChange={(event) => {
                setEndpoint(event.target.value);
                setPage(1);
              }}
            />
          </div>
          <div className={styles.filterField}>
            <label htmlFor="metrics-range">Time range</label>
            <select
              className={styles.select}
              id="metrics-range"
              value={timeRange}
              onChange={(event) => {
                setTimeRange(event.target.value);
                setPage(1);
              }}
            >
              <option value="1h">Last hour</option>
              <option value="24h">Last 24 hours</option>
              <option value="7d">Last 7 days</option>
              <option value="30d">Last 30 days</option>
            </select>
          </div>
          <div className={styles.filterField}>
            <label htmlFor="metrics-status">Status code</label>
            <select
              className={styles.select}
              id="metrics-status"
              value={statusCode}
              onChange={(event) => {
                setStatusCode(event.target.value);
                setPage(1);
              }}
            >
              <option value="">All codes</option>
              {[200, 201, 300, 400, 401, 403, 404, 429, 500].map((code) => (
                <option key={code} value={code}>
                  {code}
                </option>
              ))}
            </select>
          </div>
          <div className={styles.filterField}>
            <label htmlFor="metrics-refresh">Auto refresh</label>
            <select
              className={styles.select}
              id="metrics-refresh"
              value={refreshSeconds}
              onChange={(event) => {
                const seconds = Number(event.target.value);
                setRefreshSeconds(seconds);
                setCountdown(seconds);
              }}
            >
              <option value={0}>Off</option>
              <option value={30}>30 seconds</option>
              <option value={60}>1 minute</option>
              <option value={300}>5 minutes</option>
            </select>
          </div>
          <div className={styles.filterActions}>
            <button
              className={styles.ghostButton}
              type="button"
              onClick={clearFilters}
              disabled={!endpoint && !statusCode}
            >
              Clear filters
            </button>
            <button
              className={styles.secondaryButton}
              type="button"
              disabled={summaryQuery.isFetching || pageQuery.isFetching}
              onClick={() => {
                void refetchSummary();
                void refetchPage();
              }}
            >
              Refresh now
            </button>
          </div>
        </div>
        {refreshSeconds > 0 ? (
          <p className={styles.toolbarText} aria-live="polite">
            Next automatic refresh in {countdown} seconds.
          </p>
        ) : null}
      </section>

      {summaryQuery.isPending || pageQuery.isPending ? (
        <section className={styles.panel}>
          <TableLoader label="Loading performance metrics…" />
        </section>
      ) : firstError ? (
        <section className={styles.panel}>
          <ErrorState
            message={displayError(
              firstError,
              'Performance metrics could not be loaded.',
            )}
            onRetry={() => {
              void refetchSummary();
              void refetchPage();
            }}
          />
        </section>
      ) : (
        <>
          <section className={styles.statsGrid} aria-label="Performance summary">
            <article className={styles.statCard}>
              <span className={styles.statLabel}>Average response</span>
              <strong className={styles.statValue}>
                {formatNumber(averageResponseTime, 1)} ms
              </strong>
            </article>
            <article className={styles.statCard}>
              <span className={styles.statLabel}>Error rate</span>
              <strong className={styles.statValue}>
                {formatNumber(errorRate, 1)}%
              </strong>
            </article>
            <article className={styles.statCard}>
              <span className={styles.statLabel}>Requests per minute</span>
              <strong className={styles.statValue}>
                {formatNumber(throughput, 1)}
              </strong>
            </article>
            <article className={styles.statCard}>
              <span className={styles.statLabel}>Success rate</span>
              <strong className={styles.statValue}>
                {formatNumber(successRate, 1)}%
              </strong>
            </article>
          </section>

          {metrics.length ? (
            <section className={styles.chartGrid} aria-label="Metric charts">
              <article className={styles.chartCard}>
                <div className={styles.panelHeader}>
                  <h2>API response times</h2>
                  <div className={styles.segmented} aria-label="Chart type">
                    <button
                      className={
                        chartType === 'line'
                          ? styles.primaryButton
                          : styles.ghostButton
                      }
                      type="button"
                      onClick={() => setChartType('line')}
                    >
                      Line
                    </button>
                    <button
                      className={
                        chartType === 'bar'
                          ? styles.primaryButton
                          : styles.ghostButton
                      }
                      type="button"
                      onClick={() => setChartType('bar')}
                    >
                      Bar
                    </button>
                  </div>
                </div>
                <div className={styles.chart}>
                  {chartType === 'line' ? (
                    <Line data={responseLineData} options={lineOptions} />
                  ) : (
                    <Bar data={responseBarData} options={barOptions} />
                  )}
                </div>
              </article>
              <article className={styles.chartCard}>
                <h2>Error rate trend</h2>
                <div className={styles.chart}>
                  <Line data={errorData} options={errorOptions} />
                </div>
              </article>
            </section>
          ) : null}

          <section className={`${styles.panel} ${styles.tablePanel}`}>
            <div className={styles.panelHeader}>
              <h2>Endpoint summary</h2>
            </div>
            {endpoints.length === 0 ? (
              <EmptyState
                title="No metrics found"
                description="No performance records match the selected filters."
              />
            ) : (
              <div className={styles.tableScroll}>
                <table className={styles.table}>
                  <caption className={styles.srOnly}>
                    Aggregate performance by endpoint
                  </caption>
                  <thead>
                    <tr>
                      <th scope="col">Endpoint</th>
                      <th className={styles.numberCell} scope="col">
                        Average
                      </th>
                      <th className={styles.numberCell} scope="col">
                        Min / max
                      </th>
                      <th className={styles.numberCell} scope="col">
                        Requests
                      </th>
                      <th className={styles.numberCell} scope="col">
                        Error rate
                      </th>
                      <th scope="col">Health</th>
                      <th scope="col">Last called</th>
                      <th className={styles.actionsCell} scope="col">
                        Actions
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    {endpoints.map((item) => (
                      <tr key={item.name}>
                        <td className={styles.monospace}>{item.name}</td>
                        <td className={styles.numberCell}>
                          {formatNumber(item.avgResponseTime, 1)} ms
                        </td>
                        <td className={styles.numberCell}>
                          {formatNumber(item.minTime)} /{' '}
                          {formatNumber(item.maxTime)} ms
                        </td>
                        <td className={styles.numberCell}>
                          {formatNumber(item.requestCount)}
                        </td>
                        <td className={styles.numberCell}>
                          {formatNumber(item.errorRate, 1)}%
                        </td>
                        <td>
                          <StatusBadge value={item.healthStatus} />
                        </td>
                        <td>{formatDate(item.lastCalled)}</td>
                        <td className={styles.actionsCell}>
                          <button
                            className={`${styles.secondaryButton} ${styles.compactButton}`}
                            type="button"
                            onClick={() => setSelectedEndpoint(item)}
                          >
                            Details
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>

          <section className={`${styles.panel} ${styles.tablePanel}`}>
            <div className={styles.panelHeader}>
              <h2>Request details</h2>
              <label className={styles.filterField} htmlFor="metrics-page-size">
                <span className={styles.fieldLabel}>Rows per page</span>
                <select
                  className={styles.select}
                  id="metrics-page-size"
                  value={pageSize}
                  onChange={(event) => {
                    setPageSize(Number(event.target.value));
                    setPage(1);
                  }}
                >
                  {[10, 20, 50].map((size) => (
                    <option key={size} value={size}>
                      {size}
                    </option>
                  ))}
                </select>
              </label>
            </div>
            {requests.length === 0 ? (
              <EmptyState
                title="No requests found"
                description="No request records match the selected filters."
              />
            ) : (
              <>
                <div className={styles.tableScroll}>
                  <table className={styles.table}>
                    <caption className={styles.srOnly}>
                      Individual API performance records
                    </caption>
                    <thead>
                      <tr>
                        <th scope="col">Timestamp</th>
                        <th scope="col">Method</th>
                        <th scope="col">Endpoint</th>
                        <th className={styles.numberCell} scope="col">
                          Response
                        </th>
                        <th scope="col">Status</th>
                        <th scope="col">IP address</th>
                      </tr>
                    </thead>
                    <tbody>
                      {requests.map((request) => (
                        <tr key={request.id}>
                          <td>{formatDate(request.timestamp)}</td>
                          <td>{request.method}</td>
                          <td className={styles.monospace}>
                            {request.endpoint}
                          </td>
                          <td className={styles.numberCell}>
                            {formatNumber(request.responseTimeMs, 1)} ms
                          </td>
                          <td>
                            <StatusBadge value={String(request.statusCode)} />
                          </td>
                          <td>{request.ipAddress || '—'}</td>
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
          {lastUpdated ? (
            <p className={styles.toolbarText}>
              Last refreshed {formatDate(new Date(lastUpdated).toISOString())}
            </p>
          ) : null}
        </>
      )}

      <Modal
        open={selectedEndpoint !== null}
        title={
          selectedEndpoint
            ? `Endpoint: ${selectedEndpoint.name}`
            : 'Endpoint details'
        }
        size="lg"
        onClose={() => setSelectedEndpoint(null)}
        footer={
          <button
            className={styles.ghostButton}
            type="button"
            onClick={() => setSelectedEndpoint(null)}
          >
            Close
          </button>
        }
      >
        {selectedEndpoint ? (
          <>
            <dl className={styles.detailsGrid}>
              <div className={styles.detail}>
                <dt>Average response</dt>
                <dd>
                  {formatNumber(selectedEndpoint.avgResponseTime, 1)} ms
                </dd>
              </div>
              <div className={styles.detail}>
                <dt>Total requests in sample</dt>
                <dd>{formatNumber(selectedEndpoint.requestCount)}</dd>
              </div>
              <div className={styles.detail}>
                <dt>Success rate</dt>
                <dd>{formatNumber(selectedEndpoint.successRate, 1)}%</dd>
              </div>
              <div className={styles.detail}>
                <dt>Health</dt>
                <dd>
                  <StatusBadge value={selectedEndpoint.healthStatus} />
                </dd>
              </div>
            </dl>
            <h3>Recent requests</h3>
            {endpointQuery.isPending ? (
              <TableLoader label="Loading endpoint requests…" />
            ) : endpointQuery.isError ? (
              <ErrorState
                message={displayError(
                  endpointQuery.error,
                  'Endpoint requests could not be loaded.',
                )}
              />
            ) : (endpointQuery.data?.items.length ?? 0) === 0 ? (
              <EmptyState
                title="No requests found"
                description="No recent requests were returned for this endpoint."
              />
            ) : (
              <div className={styles.tableScroll}>
                <table className={styles.table}>
                  <caption className={styles.srOnly}>
                    Recent requests to {selectedEndpoint.name}
                  </caption>
                  <thead>
                    <tr>
                      <th scope="col">Time</th>
                      <th scope="col">Method</th>
                      <th scope="col">Status</th>
                      <th className={styles.numberCell} scope="col">
                        Response
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    {endpointQuery.data?.items.map((request) => (
                      <tr key={request.id}>
                        <td>{formatDate(request.timestamp)}</td>
                        <td>{request.method}</td>
                        <td>
                          <StatusBadge value={String(request.statusCode)} />
                        </td>
                        <td className={styles.numberCell}>
                          {formatNumber(request.responseTimeMs, 1)} ms
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </>
        ) : null}
      </Modal>
    </main>
  );
}

function buildErrorChart(metrics: SystemMetric[]): ChartData<'line'> {
  const groups = new Map<string, { total: number; errors: number }>();
  metrics.forEach((metric) => {
    const label = new Intl.DateTimeFormat('en-IN', {
      hour: '2-digit',
      minute: '2-digit',
    }).format(new Date(metric.timestamp));
    const group = groups.get(label) ?? { total: 0, errors: 0 };
    group.total += 1;
    if (metric.statusCode >= 400) group.errors += 1;
    groups.set(label, group);
  });

  return {
    labels: Array.from(groups.keys()),
    datasets: [
      {
        label: 'Error rate',
        data: Array.from(groups.values(), (group) =>
          group.total ? (group.errors / group.total) * 100 : 0,
        ),
        borderColor: '#ef4444',
        backgroundColor: 'rgb(239 68 68 / 12%)',
        tension: 0.3,
        fill: true,
      },
    ],
  };
}
