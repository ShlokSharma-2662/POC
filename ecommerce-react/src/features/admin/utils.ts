import type {
  AdminOrder,
  EndpointPerformance,
  SystemMetric,
} from './types';

type QueryValue = string | number | boolean | null | undefined;

export function buildQuery(values: Record<string, QueryValue>): string {
  const params = new URLSearchParams();
  Object.entries(values).forEach(([key, value]) => {
    if (value === undefined || value === null || value === '') return;
    params.set(key, String(value));
  });
  const query = params.toString();
  return query ? `?${query}` : '';
}

export function displayError(error: unknown, fallback: string): string {
  if (error instanceof Error && error.message.trim()) return error.message;
  return fallback;
}

export function formatCurrency(value: number): string {
  return new Intl.NumberFormat('en-IN', {
    style: 'currency',
    currency: 'INR',
    maximumFractionDigits: 2,
  }).format(Number.isFinite(value) ? value : 0);
}

export function formatNumber(value: number, maximumFractionDigits = 0): string {
  return new Intl.NumberFormat('en-IN', {
    maximumFractionDigits,
  }).format(Number.isFinite(value) ? value : 0);
}

export function formatDate(value: string, includeTime = true): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return 'Unknown';
  return new Intl.DateTimeFormat('en-IN', {
    dateStyle: 'medium',
    ...(includeTime ? { timeStyle: 'short' as const } : {}),
  }).format(date);
}

export function orderTotal(order: AdminOrder): number {
  return (order.items ?? []).reduce(
    (total, item) => total + Number(item.price || 0) * Number(item.quantity || 0),
    0,
  );
}

export function totalPages(totalCount: number, pageSize: number): number {
  return Math.max(1, Math.ceil(totalCount / Math.max(1, pageSize)));
}

export function metricsFromRange(range: string, now = new Date()): string {
  const milliseconds: Record<string, number> = {
    '1h': 60 * 60 * 1000,
    '24h': 24 * 60 * 60 * 1000,
    '7d': 7 * 24 * 60 * 60 * 1000,
    '30d': 30 * 24 * 60 * 60 * 1000,
  };
  return new Date(now.getTime() - (milliseconds[range] ?? milliseconds['24h']!)).toISOString();
}

export function aggregateEndpoints(metrics: SystemMetric[]): EndpointPerformance[] {
  const groups = new Map<string, SystemMetric[]>();
  metrics.forEach((metric) => {
    const group = groups.get(metric.endpoint) ?? [];
    group.push(metric);
    groups.set(metric.endpoint, group);
  });

  return Array.from(groups, ([name, group]): EndpointPerformance => {
    const responseTimes = group.map((item) => item.responseTimeMs);
    const errors = group.filter((item) => item.statusCode >= 400).length;
    const errorRate = group.length ? (errors / group.length) * 100 : 0;
    const timestamps = group
      .map((item) => new Date(item.timestamp).getTime())
      .filter(Number.isFinite);
    const healthStatus: EndpointPerformance['healthStatus'] =
      errorRate >= 20 ? 'Critical' : errorRate > 0 ? 'Warning' : 'Healthy';
    return {
      name,
      avgResponseTime:
        responseTimes.reduce((sum, responseTime) => sum + responseTime, 0) /
        Math.max(1, responseTimes.length),
      requestCount: group.length,
      errorRate,
      successRate: 100 - errorRate,
      minTime: Math.min(...responseTimes),
      maxTime: Math.max(...responseTimes),
      healthStatus,
      lastCalled: timestamps.length
        ? new Date(Math.max(...timestamps)).toISOString()
        : group[0]!.timestamp,
    };
  }).sort((left, right) => right.requestCount - left.requestCount);
}

export interface CsvColumn<T> {
  label: string;
  value: (row: T) => unknown;
}

function csvCell(value: unknown): string {
  const normalized =
    value === null || value === undefined
      ? ''
      : value instanceof Date
        ? value.toISOString()
        : String(value);
  return /[",\r\n]/.test(normalized)
    ? `"${normalized.replace(/"/g, '""')}"`
    : normalized;
}

export function toCsv<T>(rows: T[], columns: CsvColumn<T>[]): string {
  return [
    columns.map((column) => csvCell(column.label)).join(','),
    ...rows.map((row) =>
      columns.map((column) => csvCell(column.value(row))).join(','),
    ),
  ].join('\r\n');
}

export function downloadCsv<T>(
  filename: string,
  rows: T[],
  columns: CsvColumn<T>[],
): void {
  const blob = new Blob([`\uFEFF${toCsv(rows, columns)}`], {
    type: 'text/csv;charset=utf-8',
  });
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = filename;
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  URL.revokeObjectURL(url);
}

export function datedFilename(prefix: string): string {
  return `${prefix}_${new Date().toISOString().slice(0, 10)}.csv`;
}
