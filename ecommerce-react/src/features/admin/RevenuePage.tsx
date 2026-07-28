import { useQuery } from '@tanstack/react-query';
import type { ChartData, ChartOptions } from 'chart.js';
import { useState } from 'react';
import { Bar, Line } from 'react-chartjs-2';
import { toast } from 'sonner';
import { adminApi, type RevenueReportOptions } from './admin-api';
import {
  EmptyState,
  ErrorState,
  PageHero,
  TableLoader,
} from './admin-components';
import styles from './Admin.module.scss';
import './charts';
import {
  datedFilename,
  displayError,
  downloadCsv,
  formatCurrency,
  formatDate,
  formatNumber,
} from './utils';

const chartOptions: ChartOptions<'line'> = {
  responsive: true,
  maintainAspectRatio: false,
  interaction: { intersect: false, mode: 'index' },
  plugins: {
    legend: { position: 'top' },
    tooltip: {
      callbacks: {
        label: (context) =>
          `Revenue: ${formatCurrency(Number(context.parsed.y ?? 0))}`,
      },
    },
  },
  scales: {
    y: {
      beginAtZero: true,
      ticks: {
        callback: (value) => formatCurrency(Number(value)),
      },
    },
  },
};

const barChartOptions: ChartOptions<'bar'> = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: { position: 'top' },
    tooltip: {
      callbacks: {
        label: (context) =>
          `Revenue: ${formatCurrency(Number(context.parsed.y ?? 0))}`,
      },
    },
  },
  scales: {
    y: {
      beginAtZero: true,
      ticks: {
        callback: (value) => formatCurrency(Number(value)),
      },
    },
  },
};

const currentDate = new Date();
const initialReport: RevenueReportOptions = { reportType: '6months' };

export function RevenuePage() {
  const [summaryPeriod, setSummaryPeriod] = useState('30days');
  const [reportType, setReportType] = useState('6months');
  const [month, setMonth] = useState(currentDate.getMonth() + 1);
  const [year, setYear] = useState(currentDate.getFullYear());
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [appliedReport, setAppliedReport] =
    useState<RevenueReportOptions>(initialReport);
  const [viewMode, setViewMode] = useState<'chart' | 'table'>('chart');
  const [chartType, setChartType] = useState<'line' | 'bar'>('line');

  const summaryQuery = useQuery({
    queryKey: ['admin', 'revenue', 'summary', summaryPeriod],
    queryFn: () => adminApi.getRevenueSummary(summaryPeriod),
  });

  const reportQuery = useQuery({
    queryKey: ['admin', 'revenue', 'report', appliedReport],
    queryFn: () => adminApi.getRevenueReport(appliedReport),
  });

  const report = reportQuery.data;
  const summary = summaryQuery.data;
  const averageOrderValue =
    report && report.totalOrders
      ? report.totalRevenue / report.totalOrders
      : 0;
  const labels = report?.periods.map((period) => period.periodLabel) ?? [];
  const revenues = report?.periods.map((period) => period.revenue) ?? [];
  const lineData: ChartData<'line'> = {
    labels,
    datasets: [
      {
        label: 'Revenue',
        data: revenues,
        borderColor: '#4f46e5',
        backgroundColor: 'rgb(79 70 229 / 12%)',
        tension: 0.35,
        fill: true,
        pointBackgroundColor: '#4f46e5',
        pointBorderColor: '#fff',
        pointBorderWidth: 2,
      },
    ],
  };
  const barData: ChartData<'bar'> = {
    labels,
    datasets: [
      {
        label: 'Revenue',
        data: revenues,
        borderColor: '#4f46e5',
        backgroundColor: 'rgb(79 70 229 / 75%)',
        borderWidth: 1,
        borderRadius: 5,
      },
    ],
  };
  const years = Array.from(
    { length: 6 },
    (_, index) => currentDate.getFullYear() - index,
  );

  function applyFilters() {
    if (reportType === 'custom') {
      if (!startDate || !endDate) {
        toast.error('Select both a start and end date.');
        return;
      }
      if (new Date(startDate) > new Date(endDate)) {
        toast.error('The start date must be before the end date.');
        return;
      }
      setAppliedReport({ reportType: 'custom', startDate, endDate });
      return;
    }

    if (reportType === 'monthly') {
      setAppliedReport({ reportType, month, year });
      return;
    }
    setAppliedReport({ reportType });
  }

  function resetFilters() {
    setReportType('6months');
    setMonth(currentDate.getMonth() + 1);
    setYear(currentDate.getFullYear());
    setStartDate('');
    setEndDate('');
    setAppliedReport(initialReport);
  }

  function exportRevenue() {
    if (!report?.periods.length) return;
    downloadCsv(datedFilename(`revenue_${report.reportType}`), report.periods, [
      { label: 'Period', value: (period) => period.periodLabel },
      { label: 'Start date', value: (period) => period.startDate },
      { label: 'End date', value: (period) => period.endDate },
      { label: 'Revenue (INR)', value: (period) => period.revenue.toFixed(2) },
      { label: 'Orders', value: (period) => period.orderCount },
      {
        label: 'Average order value (INR)',
        value: (period) => period.averageOrderValue.toFixed(2),
      },
    ]);
    toast.success(`Exported ${report.periods.length} revenue periods.`);
  }

  const growthClass = (value: number) =>
    value >= 0 ? styles.positive : styles.negative;

  return (
    <main className={styles.page}>
      <PageHero
        title="Revenue reporting"
        description="Compare revenue periods, inspect order value, and export report data."
        actions={
          <button
            className={styles.secondaryButton}
            type="button"
            onClick={exportRevenue}
            disabled={!report?.periods.length}
          >
            Export report
          </button>
        }
      />

      <section className={styles.panel} aria-label="Revenue summary period">
        <div className={styles.panelHeader}>
          <h2>Summary</h2>
          <label className={styles.filterField} htmlFor="summary-period">
            <span className={styles.fieldLabel}>Comparison period</span>
            <select
              className={styles.select}
              id="summary-period"
              value={summaryPeriod}
              onChange={(event) => setSummaryPeriod(event.target.value)}
            >
              <option value="7days">Last 7 days</option>
              <option value="30days">Last 30 days</option>
              <option value="90days">Last 90 days</option>
            </select>
          </label>
        </div>
        {summaryQuery.isPending ? (
          <TableLoader label="Loading revenue summary…" />
        ) : summaryQuery.isError ? (
          <ErrorState
            message={displayError(
              summaryQuery.error,
              'The revenue summary could not be loaded.',
            )}
            onRetry={() => void summaryQuery.refetch()}
          />
        ) : summary ? (
          <div className={styles.statsGrid}>
            <article className={styles.statCard}>
              <span className={styles.statLabel}>Revenue</span>
              <strong className={styles.statValue}>
                {formatCurrency(summary.totalRevenue)}
              </strong>
              <small
                className={`${styles.statMeta} ${growthClass(summary.revenueGrowth)}`}
              >
                {summary.revenueGrowth >= 0 ? '+' : ''}
                {formatNumber(summary.revenueGrowth, 1)}% from previous period
              </small>
            </article>
            <article className={styles.statCard}>
              <span className={styles.statLabel}>Orders</span>
              <strong className={styles.statValue}>
                {formatNumber(summary.totalOrders)}
              </strong>
              <small
                className={`${styles.statMeta} ${growthClass(summary.orderGrowth)}`}
              >
                {summary.orderGrowth >= 0 ? '+' : ''}
                {formatNumber(summary.orderGrowth, 1)}% from previous period
              </small>
            </article>
            <article className={styles.statCard}>
              <span className={styles.statLabel}>Average order value</span>
              <strong className={styles.statValue}>
                {formatCurrency(summary.averageOrderValue)}
              </strong>
            </article>
            <article className={styles.statCard}>
              <span className={styles.statLabel}>Last calculated</span>
              <strong className={styles.statValue}>
                {formatDate(summary.lastUpdated, false)}
              </strong>
            </article>
          </div>
        ) : null}
      </section>

      <section className={styles.panel} aria-label="Revenue report filters">
        <div className={styles.panelHeader}>
          <h2>Report filters</h2>
        </div>
        <div className={styles.filters}>
          <div className={styles.filterField}>
            <label htmlFor="report-type">Report type</label>
            <select
              className={styles.select}
              id="report-type"
              value={reportType}
              onChange={(event) => setReportType(event.target.value)}
            >
              <option value="15day">Last 15 days</option>
              <option value="monthly">Specific month</option>
              <option value="6months">Last 6 months</option>
              <option value="12months">Last 12 months</option>
              <option value="custom">Custom date range</option>
            </select>
          </div>
          {reportType === 'monthly' ? (
            <>
              <div className={styles.filterField}>
                <label htmlFor="report-month">Month</label>
                <select
                  className={styles.select}
                  id="report-month"
                  value={month}
                  onChange={(event) => setMonth(Number(event.target.value))}
                >
                  {Array.from({ length: 12 }, (_, index) => (
                    <option value={index + 1} key={index + 1}>
                      {new Intl.DateTimeFormat('en-IN', {
                        month: 'long',
                      }).format(new Date(2024, index, 1))}
                    </option>
                  ))}
                </select>
              </div>
              <div className={styles.filterField}>
                <label htmlFor="report-year">Year</label>
                <select
                  className={styles.select}
                  id="report-year"
                  value={year}
                  onChange={(event) => setYear(Number(event.target.value))}
                >
                  {years.map((option) => (
                    <option key={option} value={option}>
                      {option}
                    </option>
                  ))}
                </select>
              </div>
            </>
          ) : null}
          {reportType === 'custom' ? (
            <>
              <div className={styles.filterField}>
                <label htmlFor="report-start">Start date</label>
                <input
                  className={styles.input}
                  id="report-start"
                  type="date"
                  value={startDate}
                  onChange={(event) => setStartDate(event.target.value)}
                />
              </div>
              <div className={styles.filterField}>
                <label htmlFor="report-end">End date</label>
                <input
                  className={styles.input}
                  id="report-end"
                  type="date"
                  value={endDate}
                  onChange={(event) => setEndDate(event.target.value)}
                />
              </div>
            </>
          ) : null}
          <div className={styles.filterActions}>
            <button
              className={styles.ghostButton}
              type="button"
              onClick={resetFilters}
            >
              Reset
            </button>
            <button
              className={styles.primaryButton}
              type="button"
              onClick={applyFilters}
              disabled={reportQuery.isFetching}
            >
              Run report
            </button>
          </div>
        </div>
      </section>

      <section className={styles.panel}>
        <div className={styles.panelHeader}>
          <h2>Revenue report</h2>
          <div className={styles.segmented}>
            <button
              className={
                viewMode === 'chart'
                  ? styles.primaryButton
                  : styles.ghostButton
              }
              type="button"
              onClick={() => setViewMode('chart')}
            >
              Chart
            </button>
            <button
              className={
                viewMode === 'table'
                  ? styles.primaryButton
                  : styles.ghostButton
              }
              type="button"
              onClick={() => setViewMode('table')}
            >
              Table
            </button>
          </div>
        </div>

        {reportQuery.isPending ? (
          <TableLoader label="Loading revenue report…" />
        ) : reportQuery.isError ? (
          <ErrorState
            message={displayError(
              reportQuery.error,
              'The revenue report could not be loaded.',
            )}
            onRetry={() => void reportQuery.refetch()}
          />
        ) : !report?.periods.length ? (
          <EmptyState
            title="No revenue data"
            description="No orders were found for the selected report period."
          />
        ) : viewMode === 'chart' ? (
          <>
            <div className={styles.panelHeader}>
              <p className={styles.toolbarText}>
                {formatDate(report.reportStartDate, false)} –{' '}
                {formatDate(report.reportEndDate, false)}
              </p>
              <div className={styles.segmented}>
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
            <div className={styles.chartWide}>
              {chartType === 'line' ? (
                <Line data={lineData} options={chartOptions} />
              ) : (
                <Bar data={barData} options={barChartOptions} />
              )}
            </div>
          </>
        ) : (
          <div className={styles.tableScroll}>
            <table className={styles.table}>
              <caption className={styles.srOnly}>
                Revenue grouped by report period
              </caption>
              <thead>
                <tr>
                  <th scope="col">Period</th>
                  <th scope="col">Start</th>
                  <th scope="col">End</th>
                  <th className={styles.numberCell} scope="col">
                    Revenue
                  </th>
                  <th className={styles.numberCell} scope="col">
                    Orders
                  </th>
                  <th className={styles.numberCell} scope="col">
                    Average order
                  </th>
                </tr>
              </thead>
              <tbody>
                {report.periods.map((period) => (
                  <tr key={`${period.startDate}-${period.endDate}`}>
                    <td>{period.periodLabel}</td>
                    <td>{formatDate(period.startDate, false)}</td>
                    <td>{formatDate(period.endDate, false)}</td>
                    <td className={styles.numberCell}>
                      {formatCurrency(period.revenue)}
                    </td>
                    <td className={styles.numberCell}>
                      {formatNumber(period.orderCount)}
                    </td>
                    <td className={styles.numberCell}>
                      {formatCurrency(period.averageOrderValue)}
                    </td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr>
                  <th colSpan={3} scope="row">
                    Total
                  </th>
                  <th className={styles.numberCell}>
                    {formatCurrency(report.totalRevenue)}
                  </th>
                  <th className={styles.numberCell}>
                    {formatNumber(report.totalOrders)}
                  </th>
                  <th className={styles.numberCell}>
                    {formatCurrency(averageOrderValue)}
                  </th>
                </tr>
              </tfoot>
            </table>
          </div>
        )}
      </section>
    </main>
  );
}
