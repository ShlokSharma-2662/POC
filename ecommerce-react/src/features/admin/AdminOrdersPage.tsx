import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query';
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
import type { AdminOrder } from './types';
import { useDebouncedValue } from './use-debounced-value';
import {
  datedFilename,
  displayError,
  downloadCsv,
  formatCurrency,
  formatDate,
  formatNumber,
  orderTotal,
  totalPages,
} from './utils';

const transitions: Record<string, string[]> = {
  Pending: ['Confirmed', 'Cancelled'],
  Confirmed: ['Shipped', 'Cancelled'],
  Shipped: ['Delivered'],
  Delivered: [],
  Cancelled: [],
};

export function AdminOrdersPage() {
  const queryClient = useQueryClient();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(() => {
    const saved = Number(localStorage.getItem('admin:orders:pageSize'));
    return [5, 10, 25, 50].includes(saved) ? saved : 10;
  });
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('');
  const [selectedOrder, setSelectedOrder] = useState<AdminOrder | null>(null);
  const [copiedId, setCopiedId] = useState('');
  const debouncedSearch = useDebouncedValue(search.trim());

  const ordersQuery = useQuery({
    queryKey: [
      'admin',
      'orders',
      page,
      pageSize,
      debouncedSearch,
      status,
    ],
    queryFn: () =>
      adminApi.getOrders({
        page,
        pageSize,
        search: debouncedSearch || undefined,
        status: status || undefined,
      }),
    placeholderData: keepPreviousData,
  });

  const statusMutation = useMutation({
    mutationFn: ({ id, nextStatus }: { id: string; nextStatus: string }) =>
      adminApi.updateOrderStatus(id, nextStatus),
    onSuccess: async (_data, variables) => {
      toast.success(`Order status changed to ${variables.nextStatus}.`);
      await queryClient.invalidateQueries({ queryKey: ['admin', 'orders'] });
      await queryClient.invalidateQueries({
        queryKey: ['admin', 'dashboard', 'orders'],
      });
    },
    onError: (error) =>
      toast.error(displayError(error, 'The order status could not be updated.')),
  });

  const orders = ordersQuery.data?.items ?? [];
  const count = ordersQuery.data?.totalCount ?? 0;
  const pages = totalPages(count, pageSize);

  function clearFilters() {
    setSearch('');
    setStatus('');
    setPage(1);
  }

  function exportOrders() {
    if (!orders.length) return;
    downloadCsv(datedFilename('orders_export'), orders, [
      { label: 'Order ID', value: (order) => order.id },
      { label: 'Customer', value: (order) => order.customerName },
      { label: 'Phone', value: (order) => order.phone },
      { label: 'Shipping address', value: (order) => order.shippingAddress },
      { label: 'Status', value: (order) => order.status },
      { label: 'Created', value: (order) => order.createdAt },
      { label: 'Items', value: (order) => order.items.length },
      { label: 'Total (INR)', value: (order) => orderTotal(order).toFixed(2) },
      {
        label: 'Item details',
        value: (order) =>
          order.items
            .map(
              (item) =>
                `${item.productName} x ${item.quantity} @ ${item.price.toFixed(2)}`,
            )
            .join('; '),
      },
    ]);
    toast.success(`Exported ${orders.length} orders.`);
  }

  async function copyOrderId(id: string) {
    try {
      await navigator.clipboard.writeText(id);
      setCopiedId(id);
      window.setTimeout(
        () => setCopiedId((current) => (current === id ? '' : current)),
        1500,
      );
    } catch {
      toast.error('The order ID could not be copied.');
    }
  }

  return (
    <main className={styles.page}>
      <PageHero
        title="Manage orders"
        description="Search customer orders, review line items, and update fulfilment status."
        actions={
          <button
            className={styles.secondaryButton}
            type="button"
            onClick={exportOrders}
            disabled={!orders.length}
          >
            Export current page
          </button>
        }
      />

      <section className={styles.panel} aria-label="Order filters">
        <div className={styles.filters}>
          <div className={styles.filterField}>
            <label htmlFor="orders-search">Search orders</label>
            <input
              className={styles.input}
              id="orders-search"
              type="search"
              value={search}
              placeholder="Customer, phone, address, or order ID"
              onChange={(event) => {
                setSearch(event.target.value);
                setPage(1);
              }}
            />
          </div>
          <div className={styles.filterField}>
            <label htmlFor="orders-status">Status</label>
            <select
              className={styles.select}
              id="orders-status"
              value={status}
              onChange={(event) => {
                setStatus(event.target.value);
                setPage(1);
              }}
            >
              <option value="">All statuses</option>
              <option value="Pending">Pending</option>
              <option value="Confirmed">Confirmed</option>
              <option value="Shipped">Shipped</option>
              <option value="Delivered">Delivered</option>
              <option value="Cancelled">Cancelled</option>
            </select>
          </div>
          <div className={styles.filterField}>
            <label htmlFor="orders-page-size">Rows per page</label>
            <select
              className={styles.select}
              id="orders-page-size"
              value={pageSize}
              onChange={(event) => {
                const size = Number(event.target.value);
                setPageSize(size);
                setPage(1);
                localStorage.setItem('admin:orders:pageSize', String(size));
              }}
            >
              {[5, 10, 25, 50].map((size) => (
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
              disabled={!search && !status}
            >
              Clear
            </button>
            <button
              className={styles.secondaryButton}
              type="button"
              onClick={() => void ordersQuery.refetch()}
              disabled={ordersQuery.isFetching}
            >
              Refresh
            </button>
          </div>
        </div>
      </section>

      <section className={`${styles.panel} ${styles.tablePanel}`}>
        {ordersQuery.isPending ? (
          <TableLoader label="Loading orders…" />
        ) : ordersQuery.isError ? (
          <ErrorState
            message={displayError(
              ordersQuery.error,
              'Orders could not be loaded.',
            )}
            onRetry={() => void ordersQuery.refetch()}
          />
        ) : orders.length === 0 ? (
          <EmptyState
            title="No orders found"
            description="Try changing the search or status filter."
          />
        ) : (
          <>
            <div className={styles.tableScroll}>
              <table className={styles.table}>
                <caption className={styles.srOnly}>
                  Customer orders and fulfilment status
                </caption>
                <thead>
                  <tr>
                    <th scope="col">Order ID</th>
                    <th scope="col">Customer</th>
                    <th scope="col">Phone</th>
                    <th scope="col">Created</th>
                    <th scope="col">Status</th>
                    <th className={styles.numberCell} scope="col">
                      Items
                    </th>
                    <th className={styles.numberCell} scope="col">
                      Total
                    </th>
                    <th className={styles.actionsCell} scope="col">
                      Actions
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {orders.map((order) => {
                    const nextStatuses = transitions[order.status] ?? [];
                    const updating =
                      statusMutation.isPending &&
                      statusMutation.variables?.id === order.id;
                    return (
                      <tr key={order.id}>
                        <td>
                          <span
                            className={`${styles.monospace} ${styles.truncate}`}
                            title={order.id}
                          >
                            {order.id}
                          </span>{' '}
                          <button
                            className={`${styles.ghostButton} ${styles.compactButton}`}
                            type="button"
                            onClick={() => void copyOrderId(order.id)}
                            aria-label={`Copy order ID ${order.id}`}
                          >
                            {copiedId === order.id ? 'Copied' : 'Copy'}
                          </button>
                        </td>
                        <td>{order.customerName}</td>
                        <td>{order.phone}</td>
                        <td>{formatDate(order.createdAt, false)}</td>
                        <td>
                          {nextStatuses.length ? (
                            <>
                              <label
                                className={styles.srOnly}
                                htmlFor={`status-${order.id}`}
                              >
                                Status for order {order.id}
                              </label>
                              <select
                                className={styles.statusSelect}
                                id={`status-${order.id}`}
                                value={order.status}
                                disabled={updating}
                                onChange={(event) =>
                                  statusMutation.mutate({
                                    id: order.id,
                                    nextStatus: event.target.value,
                                  })
                                }
                              >
                                <option value={order.status}>
                                  {updating ? 'Updating…' : order.status}
                                </option>
                                {nextStatuses.map((nextStatus) => (
                                  <option key={nextStatus} value={nextStatus}>
                                    Move to {nextStatus}
                                  </option>
                                ))}
                              </select>
                            </>
                          ) : (
                            <StatusBadge value={order.status} />
                          )}
                        </td>
                        <td className={styles.numberCell}>
                          {formatNumber(order.items.length)}
                        </td>
                        <td className={styles.numberCell}>
                          {formatCurrency(orderTotal(order))}
                        </td>
                        <td className={styles.actionsCell}>
                          <button
                            className={`${styles.secondaryButton} ${styles.compactButton}`}
                            type="button"
                            onClick={() => setSelectedOrder(order)}
                          >
                            View
                          </button>
                        </td>
                      </tr>
                    );
                  })}
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
                totalPages={pages}
                onPageChange={setPage}
              />
            </div>
          </>
        )}
      </section>

      <Modal
        open={selectedOrder !== null}
        title={selectedOrder ? `Order ${selectedOrder.id}` : 'Order details'}
        size="lg"
        onClose={() => setSelectedOrder(null)}
        footer={
          <button
            className={styles.ghostButton}
            type="button"
            onClick={() => setSelectedOrder(null)}
          >
            Close
          </button>
        }
      >
        {selectedOrder ? (
          <>
            <dl className={styles.detailsGrid}>
              <div className={styles.detail}>
                <dt>Customer</dt>
                <dd>{selectedOrder.customerName}</dd>
              </div>
              <div className={styles.detail}>
                <dt>Status</dt>
                <dd>
                  <StatusBadge value={selectedOrder.status} />
                </dd>
              </div>
              <div className={styles.detail}>
                <dt>Phone</dt>
                <dd>{selectedOrder.phone}</dd>
              </div>
              <div className={styles.detail}>
                <dt>Created</dt>
                <dd>{formatDate(selectedOrder.createdAt)}</dd>
              </div>
              <div className={`${styles.detail} ${styles.wideField}`}>
                <dt>Shipping address</dt>
                <dd>{selectedOrder.shippingAddress}</dd>
              </div>
            </dl>
            <h3>Items</h3>
            <ul className={styles.itemList}>
              {selectedOrder.items.map((item) => (
                <li
                  className={styles.itemRow}
                  key={`${item.productId}-${item.productName}`}
                >
                  <div>
                    <strong>{item.productName}</strong>
                    <p>{item.description}</p>
                    <p>
                      {item.quantity} × {formatCurrency(item.price)}
                    </p>
                  </div>
                  <strong>{formatCurrency(item.price * item.quantity)}</strong>
                </li>
              ))}
            </ul>
            <p className={styles.numberCell}>
              <strong>Total: {formatCurrency(orderTotal(selectedOrder))}</strong>
            </p>
          </>
        ) : null}
      </Modal>
    </main>
  );
}
