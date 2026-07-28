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
import type { AdminUser } from './types';
import { useDebouncedValue } from './use-debounced-value';
import {
  datedFilename,
  displayError,
  downloadCsv,
  formatDate,
  formatNumber,
  totalPages,
} from './utils';

type UserAction =
  | { kind: 'role'; user: AdminUser }
  | { kind: 'reset'; user: AdminUser }
  | { kind: 'activate'; user: AdminUser }
  | { kind: 'deactivate'; user: AdminUser };

export function AdminUsersPage() {
  const queryClient = useQueryClient();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [search, setSearch] = useState('');
  const [role, setRole] = useState('');
  const [status, setStatus] = useState('');
  const [action, setAction] = useState<UserAction | null>(null);
  const [nextRole, setNextRole] = useState('User');
  const [newPassword, setNewPassword] = useState('');
  const debouncedSearch = useDebouncedValue(search.trim());

  const usersQuery = useQuery({
    queryKey: [
      'admin',
      'users',
      page,
      pageSize,
      debouncedSearch,
      role,
      status,
    ],
    queryFn: () =>
      adminApi.getUsers({
        page,
        pageSize,
        search: debouncedSearch || undefined,
        role: role || undefined,
        status: status || undefined,
      }),
    placeholderData: keepPreviousData,
  });

  const actionMutation = useMutation({
    mutationFn: async (pending: UserAction) => {
      switch (pending.kind) {
        case 'role':
          return adminApi.assignRole(pending.user.id, nextRole);
        case 'reset':
          return adminApi.resetPassword(pending.user.id, newPassword);
        case 'activate':
          return adminApi.activateUser(pending.user.id);
        case 'deactivate':
          return adminApi.deactivateUser(pending.user.id);
      }
    },
    onSuccess: async (_data, pending) => {
      const messages: Record<UserAction['kind'], string> = {
        role: `Role changed to ${nextRole}.`,
        reset: 'Password reset successfully.',
        activate: 'User activated.',
        deactivate: 'User deactivated.',
      };
      toast.success(messages[pending.kind]);
      closeAction();
      await queryClient.invalidateQueries({ queryKey: ['admin', 'users'] });
      await queryClient.invalidateQueries({
        queryKey: ['admin', 'dashboard', 'users'],
      });
    },
    onError: (error) =>
      toast.error(displayError(error, 'The user could not be updated.')),
  });

  const users = usersQuery.data?.items ?? [];
  const count = usersQuery.data?.totalCount ?? 0;

  function openAction(nextAction: UserAction) {
    setAction(nextAction);
    setNextRole(nextAction.user.role === 'Admin' ? 'User' : 'Admin');
    setNewPassword('');
  }

  function closeAction() {
    setAction(null);
    setNewPassword('');
  }

  function clearFilters() {
    setSearch('');
    setRole('');
    setStatus('');
    setPage(1);
  }

  function exportUsers() {
    if (!users.length) return;
    downloadCsv(datedFilename('users_export'), users, [
      { label: 'User ID', value: (user) => user.id },
      { label: 'Username', value: (user) => user.username },
      { label: 'Email', value: (user) => user.email },
      { label: 'Role', value: (user) => user.role },
      { label: 'Status', value: (user) => user.status },
      { label: 'Created', value: (user) => user.createdAt },
      { label: 'Last updated', value: (user) => user.updatedAt },
    ]);
    toast.success(`Exported ${users.length} users.`);
  }

  const modalTitle =
    action?.kind === 'role'
      ? 'Change user role'
      : action?.kind === 'reset'
        ? 'Reset password'
        : action?.kind === 'activate'
          ? 'Activate user'
          : 'Deactivate user';

  const canSubmit =
    action?.kind !== 'reset' || newPassword.trim().length >= 8;

  return (
    <main className={styles.page}>
      <PageHero
        title="Manage users"
        description="Review accounts, assign permissions, and control account access."
        actions={
          <button
            className={styles.secondaryButton}
            type="button"
            onClick={exportUsers}
            disabled={!users.length}
          >
            Export current page
          </button>
        }
      />

      <section className={styles.panel} aria-label="User filters">
        <div className={styles.filters}>
          <div className={styles.filterField}>
            <label htmlFor="users-search">Search users</label>
            <input
              className={styles.input}
              id="users-search"
              type="search"
              value={search}
              placeholder="Name, email, or username"
              onChange={(event) => {
                setSearch(event.target.value);
                setPage(1);
              }}
            />
          </div>
          <div className={styles.filterField}>
            <label htmlFor="users-role">Role</label>
            <select
              className={styles.select}
              id="users-role"
              value={role}
              onChange={(event) => {
                setRole(event.target.value);
                setPage(1);
              }}
            >
              <option value="">All roles</option>
              <option value="Admin">Admin</option>
              <option value="User">User</option>
            </select>
          </div>
          <div className={styles.filterField}>
            <label htmlFor="users-status">Status</label>
            <select
              className={styles.select}
              id="users-status"
              value={status}
              onChange={(event) => {
                setStatus(event.target.value);
                setPage(1);
              }}
            >
              <option value="">All statuses</option>
              <option value="Active">Active</option>
              <option value="Deactivated">Deactivated</option>
            </select>
          </div>
          <div className={styles.filterField}>
            <label htmlFor="users-page-size">Rows per page</label>
            <select
              className={styles.select}
              id="users-page-size"
              value={pageSize}
              onChange={(event) => {
                setPageSize(Number(event.target.value));
                setPage(1);
              }}
            >
              {[10, 25, 50].map((size) => (
                <option value={size} key={size}>
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
              disabled={!search && !role && !status}
            >
              Clear
            </button>
            <button
              className={styles.secondaryButton}
              type="button"
              onClick={() => void usersQuery.refetch()}
              disabled={usersQuery.isFetching}
            >
              Refresh
            </button>
          </div>
        </div>
      </section>

      <section className={`${styles.panel} ${styles.tablePanel}`}>
        {usersQuery.isPending ? (
          <TableLoader label="Loading users…" />
        ) : usersQuery.isError ? (
          <ErrorState
            message={displayError(usersQuery.error, 'Users could not be loaded.')}
            onRetry={() => void usersQuery.refetch()}
          />
        ) : users.length === 0 ? (
          <EmptyState
            title="No users found"
            description="Try changing the search, role, or status filter."
          />
        ) : (
          <>
            <div className={styles.tableScroll}>
              <table className={styles.table}>
                <caption className={styles.srOnly}>
                  User accounts, roles, and status
                </caption>
                <thead>
                  <tr>
                    <th scope="col">ID</th>
                    <th scope="col">Username</th>
                    <th scope="col">Email</th>
                    <th scope="col">Role</th>
                    <th scope="col">Status</th>
                    <th scope="col">Created</th>
                    <th className={styles.actionsCell} scope="col">
                      Actions
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {users.map((user) => (
                    <tr key={user.id}>
                      <td>{user.id}</td>
                      <td>{user.username}</td>
                      <td>{user.email}</td>
                      <td>
                        <button
                          className={styles.ghostButton}
                          type="button"
                          disabled={user.status === 'Deactivated'}
                          onClick={() => openAction({ kind: 'role', user })}
                          aria-label={`Change role for ${user.email}, currently ${user.role}`}
                        >
                          <StatusBadge value={user.role} />
                        </button>
                      </td>
                      <td>
                        <button
                          className={styles.ghostButton}
                          type="button"
                          onClick={() =>
                            openAction({
                              kind:
                                user.status === 'Active'
                                  ? 'deactivate'
                                  : 'activate',
                              user,
                            })
                          }
                          aria-label={`${user.status === 'Active' ? 'Deactivate' : 'Activate'} ${user.email}`}
                        >
                          <StatusBadge value={user.status} />
                        </button>
                      </td>
                      <td>{formatDate(user.createdAt, false)}</td>
                      <td className={styles.actionsCell}>
                        <button
                          className={`${styles.secondaryButton} ${styles.compactButton}`}
                          type="button"
                          disabled={user.status === 'Deactivated'}
                          onClick={() => openAction({ kind: 'reset', user })}
                        >
                          Reset password
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
        open={action !== null}
        title={modalTitle}
        size="sm"
        onClose={closeAction}
        footer={
          <>
            <button
              className={styles.ghostButton}
              type="button"
              onClick={closeAction}
              disabled={actionMutation.isPending}
            >
              Cancel
            </button>
            <button
              className={
                action?.kind === 'deactivate'
                  ? styles.dangerButton
                  : styles.primaryButton
              }
              type="button"
              disabled={!action || !canSubmit || actionMutation.isPending}
              onClick={() => {
                if (action) actionMutation.mutate(action);
              }}
            >
              {actionMutation.isPending ? 'Saving…' : 'Confirm'}
            </button>
          </>
        }
      >
        {action ? (
          <>
            <p>
              Account: <strong>{action.user.email}</strong>
            </p>
            {action.kind === 'role' ? (
              <div className={styles.formField}>
                <label htmlFor="new-role">New role</label>
                <select
                  className={styles.select}
                  id="new-role"
                  value={nextRole}
                  onChange={(event) => setNextRole(event.target.value)}
                >
                  <option value="Admin">Admin</option>
                  <option value="User">User</option>
                </select>
              </div>
            ) : null}
            {action.kind === 'reset' ? (
              <div className={styles.formField}>
                <label htmlFor="new-password">New password</label>
                <input
                  className={styles.input}
                  id="new-password"
                  type="password"
                  autoComplete="new-password"
                  minLength={8}
                  value={newPassword}
                  onChange={(event) => setNewPassword(event.target.value)}
                  aria-describedby="password-help"
                />
                <small id="password-help">Use at least 8 characters.</small>
              </div>
            ) : null}
            {action.kind === 'deactivate' ? (
              <p>
                This user will no longer be able to sign in until the account is
                activated again.
              </p>
            ) : null}
            {action.kind === 'activate' ? (
              <p>This user will regain access to the storefront.</p>
            ) : null}
          </>
        ) : null}
      </Modal>
    </main>
  );
}
