import { useState } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';
import { useQuery } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import { z } from 'zod';
import { Loader } from '../../components/ui/Loader';
import { Modal } from '../../components/ui/Modal';
import { api } from '../../lib/api-client';
import { getSubjectFromToken } from '../../lib/jwt';
import { userQueryKeys } from '../../lib/user-query';
import { authService } from '../../services/auth-service';
import { useAuthStore } from '../../stores/auth-store';
import type { UserProfile } from '../../types/domain';
import styles from './Storefront.module.scss';
import {
  expectedDelivery,
  formatCurrency,
  formatDate,
  getErrorMessage,
  orderTotal,
  statusTone,
  type StorefrontOrder,
  type StorefrontOrderItem,
  type StorefrontProfile,
} from './storefront-utils';

function RequireAccount({ children }: { children: React.ReactNode }) {
  const location = useLocation();
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  return isAuthenticated ? (
    children
  ) : (
    <Navigate
      to="/login"
      replace
      state={{ returnTo: `${location.pathname}${location.search}` }}
    />
  );
}

function OrderItemImage({ item }: { item: StorefrontOrderItem }) {
  const [failed, setFailed] = useState(false);

  return item.imageUrl && !failed ? (
    <img
      className={styles.orderItemImage}
      src={item.imageUrl}
      alt={item.productName}
      onError={() => setFailed(true)}
    />
  ) : (
    <div
      className={`${styles.imagePlaceholder} ${styles.orderItemImage}`}
      role="img"
      aria-label={`${item.productName} image unavailable`}
    >
      <span aria-hidden="true">◇</span>
    </div>
  );
}

function OrderItems({ order }: { order: StorefrontOrder }) {
  return (
    <div className={styles.orderItems}>
      {order.items.map((item, index) => (
        <div className={styles.orderItem} key={`${item.productId}-${index}`}>
          <OrderItemImage item={item} />
          <div>
            <strong>{item.productName}</strong>
            <span>
              {formatCurrency(item.price)} × {item.quantity}
            </span>
          </div>
          <strong>{formatCurrency(item.price * item.quantity)}</strong>
        </div>
      ))}
    </div>
  );
}

export function OrdersPage() {
  const token = useAuthStore((state) => state.token);
  const subject = getSubjectFromToken(token);
  const [selection, setSelection] = useState<{
    subject: string;
    order: StorefrontOrder;
  } | null>(null);
  const selectedOrder =
    selection?.subject === subject ? selection.order : null;

  const ordersQuery = useQuery({
    queryKey: userQueryKeys.orders(subject ?? 'missing-subject'),
    queryFn: () => api.get<StorefrontOrder[]>('/orders/my-orders'),
    enabled: Boolean(subject),
  });
  const orders = ordersQuery.data ?? [];

  return (
    <RequireAccount>
      <div className="container page">
        <header className="page-header">
          <div>
            <h1 className="page-title">My orders</h1>
            <p className="page-subtitle">
              Review order details and current fulfillment status.
            </p>
          </div>
          <Link className="btn btn-outline-primary" to="/products">
            Continue shopping
          </Link>
        </header>

        {ordersQuery.isPending ? (
          <Loader label="Loading your orders…" />
        ) : ordersQuery.isError ? (
          <div className={styles.inlineError} role="alert">
            <h2>We could not load your orders</h2>
            <p>
              {getErrorMessage(
                ordersQuery.error,
                'Order history is temporarily unavailable.',
              )}
            </p>
            <button
              className="btn btn-outline-primary"
              type="button"
              onClick={() => void ordersQuery.refetch()}
            >
              Try again
            </button>
          </div>
        ) : orders.length === 0 ? (
          <div className="empty-state surface">
            <h2>No orders yet</h2>
            <p>When you complete checkout, your orders will appear here.</p>
            <Link className="btn btn-primary" to="/products">
              Explore products
            </Link>
          </div>
        ) : (
          <div className={styles.ordersList}>
            {orders.map((order) => {
              const tone = statusTone(order.status || 'Pending');
              return (
                <article className={`${styles.orderCard} surface`} key={order.id}>
                  <header className={styles.orderHeader}>
                    <div>
                      <span>Order</span>
                      <strong>#{order.id}</strong>
                      <small>{formatDate(order.createdAt)}</small>
                    </div>
                    <span
                      className={`${styles.statusPill} ${styles[`tone${tone}`]}`}
                    >
                      {order.status || 'Pending'}
                    </span>
                  </header>
                  <div className={styles.orderCustomer}>
                    <span>
                      <strong>Customer</strong>
                      {order.customerName}
                    </span>
                    <span>
                      <strong>Phone</strong>
                      {order.phone}
                    </span>
                    <span>
                      <strong>Deliver to</strong>
                      {order.shippingAddress}
                    </span>
                  </div>
                  <OrderItems order={order} />
                  <footer className={styles.orderFooter}>
                    <div>
                      <span>Expected delivery</span>
                      <strong>{expectedDelivery(order.createdAt)}</strong>
                    </div>
                    <div>
                      <span>Total</span>
                      <strong>{formatCurrency(orderTotal(order))}</strong>
                    </div>
                    <button
                      className="btn btn-outline-primary"
                      type="button"
                      onClick={() => {
                        if (subject) setSelection({ subject, order });
                      }}
                    >
                      View details
                    </button>
                  </footer>
                </article>
              );
            })}
          </div>
        )}

        <Modal
          open={selectedOrder !== null}
          title="Order details"
          size="lg"
          onClose={() => setSelection(null)}
          footer={
            <button
              className="btn btn-primary"
              type="button"
              onClick={() => setSelection(null)}
            >
              Close
            </button>
          }
        >
          {selectedOrder ? (
            <div className={styles.orderModal}>
              <div className={styles.orderNumber}>
                <span>Order ID</span>
                <strong>{selectedOrder.id}</strong>
              </div>
              <dl className={styles.detailList}>
                <div>
                  <dt>Customer</dt>
                  <dd>{selectedOrder.customerName}</dd>
                </div>
                <div>
                  <dt>Phone</dt>
                  <dd>{selectedOrder.phone}</dd>
                </div>
                <div>
                  <dt>Shipping address</dt>
                  <dd>{selectedOrder.shippingAddress}</dd>
                </div>
                <div>
                  <dt>Order date</dt>
                  <dd>{formatDate(selectedOrder.createdAt)}</dd>
                </div>
                <div>
                  <dt>Status</dt>
                  <dd>{selectedOrder.status || 'Pending'}</dd>
                </div>
              </dl>
              <OrderItems order={selectedOrder} />
              <div className={styles.modalTotal}>
                <span>Total amount</span>
                <strong>{formatCurrency(orderTotal(selectedOrder))}</strong>
              </div>
            </div>
          ) : null}
        </Modal>
      </div>
    </RequireAccount>
  );
}

type ProfilePayload = UserProfile & {
  id?: number;
  status?: string;
  updatedAt?: string;
};

function normalizeProfile(profile: UserProfile): StorefrontProfile {
  const payload = profile as ProfilePayload;
  return {
    id: Number(payload.id ?? payload.userId ?? 0),
    firstName: payload.firstName,
    lastName: payload.lastName,
    email: payload.email,
    role: payload.role,
    status: payload.status ?? 'Active',
    createdAt: payload.createdAt,
    updatedAt: payload.updatedAt,
  };
}

export function ProfilePage() {
  const token = useAuthStore((state) => state.token);
  const subject = getSubjectFromToken(token);
  const profileQuery = useQuery({
    queryKey: userQueryKeys.profile(subject ?? 'missing-subject'),
    queryFn: () => authService.profile().then(normalizeProfile),
    enabled: Boolean(subject),
  });
  const profile = profileQuery.data;

  return (
    <RequireAccount>
      <div className="container page">
        <header className="page-header">
          <div>
            <h1 className="page-title">My profile</h1>
            <p className="page-subtitle">
              Your account and membership information.
            </p>
          </div>
        </header>

        {profileQuery.isPending ? (
          <Loader label="Loading your profile…" fullPage />
        ) : profileQuery.isError ? (
          <div className={styles.inlineError} role="alert">
            <h2>We could not load your profile</h2>
            <p>{getErrorMessage(profileQuery.error)}</p>
            <button
              className="btn btn-outline-primary"
              type="button"
              onClick={() => void profileQuery.refetch()}
            >
              Try again
            </button>
          </div>
        ) : profile ? (
          <section className={`${styles.profileCard} surface`}>
            <header className={styles.profileHero}>
              <div className={styles.avatar} aria-hidden="true">
                {profile.firstName.charAt(0)}
                {profile.lastName.charAt(0)}
              </div>
              <div>
                <h2>
                  {profile.firstName} {profile.lastName}
                </h2>
                <p>{profile.email}</p>
              </div>
              <span
                className={`${styles.statusPill} ${
                  profile.status.toLowerCase() === 'active'
                    ? styles.tonesuccess
                    : styles.tonedanger
                }`}
              >
                {profile.status}
              </span>
            </header>

            <div className={styles.profileSections}>
              <section>
                <h3>Personal information</h3>
                <dl className={styles.detailList}>
                  <div>
                    <dt>First name</dt>
                    <dd>{profile.firstName}</dd>
                  </div>
                  <div>
                    <dt>Last name</dt>
                    <dd>{profile.lastName}</dd>
                  </div>
                  <div>
                    <dt>Email address</dt>
                    <dd>{profile.email}</dd>
                  </div>
                  <div>
                    <dt>Account role</dt>
                    <dd>
                      {profile.role === 'Admin' ? 'Administrator' : 'Customer'}
                    </dd>
                  </div>
                </dl>
              </section>
              <section>
                <h3>Account information</h3>
                <dl className={styles.detailList}>
                  <div>
                    <dt>Member since</dt>
                    <dd>{formatDate(profile.createdAt)}</dd>
                  </div>
                  <div>
                    <dt>Last updated</dt>
                    <dd>{formatDate(profile.updatedAt)}</dd>
                  </div>
                  <div>
                    <dt>User ID</dt>
                    <dd>{profile.id || 'Not available'}</dd>
                  </div>
                  <div>
                    <dt>Status</dt>
                    <dd>{profile.status}</dd>
                  </div>
                </dl>
              </section>
            </div>

            <footer className={styles.profileActions}>
              <Link className="btn btn-outline-primary" to="/my-orders">
                View my orders
              </Link>
              <Link className="btn btn-outline-primary" to="/wishlist">
                My wishlist
              </Link>
              <Link className="btn btn-outline-warning" to="/change-password">
                Change password
              </Link>
              <Link className="btn btn-primary" to="/home">
                Back to home
              </Link>
            </footer>
          </section>
        ) : null}
      </div>
    </RequireAccount>
  );
}

const changePasswordSchema = z
  .object({
    oldPassword: z.string().min(1, 'Enter your current password.'),
    newPassword: z.string().min(8, 'Use at least 8 characters.'),
    confirmPassword: z.string().min(1, 'Confirm your new password.'),
  })
  .superRefine((values, context) => {
    if (values.newPassword !== values.confirmPassword) {
      context.addIssue({
        code: 'custom',
        path: ['confirmPassword'],
        message: 'The new passwords do not match.',
      });
    }
    if (values.oldPassword === values.newPassword) {
      context.addIssue({
        code: 'custom',
        path: ['newPassword'],
        message: 'Choose a password different from your current password.',
      });
    }
  });

type ChangePasswordValues = z.infer<typeof changePasswordSchema>;

export function ChangePasswordPage() {
  const navigate = useNavigate();
  const [serverError, setServerError] = useState('');
  const [visibility, setVisibility] = useState({
    oldPassword: false,
    newPassword: false,
    confirmPassword: false,
  });
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ChangePasswordValues>({
    resolver: zodResolver(changePasswordSchema),
    defaultValues: {
      oldPassword: '',
      newPassword: '',
      confirmPassword: '',
    },
  });

  const submit = async (values: ChangePasswordValues) => {
    setServerError('');
    try {
      await authService.changePassword(values);
      toast.success('Your password was changed successfully.');
      navigate('/profile', { replace: true });
    } catch (error) {
      setServerError(
        getErrorMessage(error, 'Failed to change your password.'),
      );
    }
  };

  const fields: Array<{
    name: keyof ChangePasswordValues;
    label: string;
    autoComplete: string;
  }> = [
    {
      name: 'oldPassword',
      label: 'Current password',
      autoComplete: 'current-password',
    },
    {
      name: 'newPassword',
      label: 'New password',
      autoComplete: 'new-password',
    },
    {
      name: 'confirmPassword',
      label: 'Confirm new password',
      autoComplete: 'new-password',
    },
  ];

  return (
    <RequireAccount>
      <div className={`${styles.passwordPage} container page`}>
        <section className={`${styles.passwordCard} surface`}>
          <div className={styles.authHeading}>
            <span className={styles.lockMark} aria-hidden="true">
              ◈
            </span>
            <h1>Change password</h1>
            <p>Use at least eight characters and avoid reusing the old password.</p>
          </div>
          {serverError ? (
            <div className="alert alert-danger" role="alert">
              {serverError}
            </div>
          ) : null}
          <form
            className={styles.authForm}
            onSubmit={handleSubmit(submit)}
            noValidate
          >
            {fields.map((field) => (
              <label
                className={styles.formField}
                htmlFor={field.name}
                key={field.name}
              >
                <span>{field.label}</span>
                <div className={styles.passwordField}>
                  <input
                    {...register(field.name)}
                    id={field.name}
                    className={`form-control ${
                      errors[field.name] ? 'is-invalid' : ''
                    }`}
                    type={visibility[field.name] ? 'text' : 'password'}
                    autoComplete={field.autoComplete}
                  />
                  <button
                    type="button"
                    onClick={() =>
                      setVisibility((current) => ({
                        ...current,
                        [field.name]: !current[field.name],
                      }))
                    }
                  >
                    {visibility[field.name] ? 'Hide' : 'Show'}
                  </button>
                </div>
                {errors[field.name] ? (
                  <small className={styles.fieldError}>
                    {errors[field.name]?.message}
                  </small>
                ) : null}
              </label>
            ))}
            <div className={styles.formActions}>
              <button
                className="btn btn-outline-secondary"
                type="button"
                disabled={isSubmitting}
                onClick={() => navigate('/profile')}
              >
                Cancel
              </button>
              <button
                className="btn btn-primary"
                type="submit"
                disabled={isSubmitting}
              >
                {isSubmitting ? 'Changing…' : 'Change password'}
              </button>
            </div>
          </form>
        </section>
      </div>
    </RequireAccount>
  );
}

export function UnauthorizedPage() {
  return (
    <div className={`${styles.unauthorizedPage} container page`}>
      <section className={`${styles.unauthorizedCard} surface`}>
        <div className={styles.errorMark} aria-hidden="true">
          !
        </div>
        <span className={styles.eyebrow}>Access denied</span>
        <h1>You don’t have permission to view this page.</h1>
        <p>
          Return to the storefront, or contact support if you believe your
          account should have access.
        </p>
        <div>
          <Link className="btn btn-primary" to="/home">
            Go to home
          </Link>
          <a className="btn btn-outline-primary" href="mailto:support@eshop.com">
            Contact support
          </a>
        </div>
      </section>
    </div>
  );
}
