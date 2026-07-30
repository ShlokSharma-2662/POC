import { useEffect, useMemo, useState } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';
import {
  CardElement,
  Elements,
  useElements,
  useStripe,
} from '@stripe/react-stripe-js';
import { loadStripe } from '@stripe/stripe-js';
import { useForm } from 'react-hook-form';
import {
  Link,
  Navigate,
  useLocation,
  useNavigate,
  useSearchParams,
} from 'react-router-dom';
import { toast } from 'sonner';
import { z } from 'zod';
import { Loader } from '../../components/ui/Loader';
import { Modal } from '../../components/ui/Modal';
import { environment } from '../../config/environment';
import { api } from '../../lib/api-client';
import { getSubjectFromToken } from '../../lib/jwt';
import {
  readJson,
  storageKeys,
  writeJson,
} from '../../lib/storage';
import { authService } from '../../services/auth-service';
import { useAuthStore } from '../../stores/auth-store';
import {
  cartTotal,
  useCartStore,
} from '../../stores/cart-store';
import type {
  CartItem,
  CheckoutRequest,
  CheckoutResponse,
} from '../../types/domain';
import styles from './Storefront.module.scss';
import {
  formatCurrency,
  getErrorMessage,
  type LastOrderSummary,
} from './storefront-utils';

interface PaymentIntentResponse {
  paymentIntentId: string;
  clientSecret: string;
  amount: number;
  currency: string;
  cartFingerprint: string;
}

const checkoutSchema = z.object({
  fullName: z.string().trim().min(2, 'Enter the recipient’s full name.'),
  address: z.string().trim().min(5, 'Enter a complete shipping address.'),
  phoneNumber: z
    .string()
    .trim()
    .refine(
      (value) => /^\d{10}$/.test(value.replace(/\D/g, '')),
      'Enter a valid 10-digit phone number.',
    ),
});

type CheckoutValues = z.infer<typeof checkoutSchema>;

const stripePromise = environment.stripePublishableKey
  ? loadStripe(environment.stripePublishableKey)
  : Promise.resolve(null);

const checkoutSteps = ['Delivery details', 'Secure payment', 'Order submitted'];

function CheckoutProgress({
  activeStep,
  message,
}: {
  activeStep: 1 | 2 | 3;
  message: string;
}) {
  return (
    <div className={styles.checkoutProgress} aria-label="Checkout progress">
      <span className={styles.checkoutNotice} role="status">
        {message}
      </span>
      <ol>
        {checkoutSteps.map((label, index) => {
          const current = (index + 1) as 1 | 2 | 3;
          return (
            <li
              className={`${styles.checkoutStep} ${
                current === activeStep ? styles.checkoutStepActive : ''
              }`}
              key={label}
            >
              <span>{current}</span>
              <strong>{label}</strong>
            </li>
          );
        })}
      </ol>
    </div>
  );
}

function CartImage({
  item,
  className,
}: {
  item: CartItem;
  className?: string;
}) {
  const [failedSource, setFailedSource] = useState<string | undefined>();
  const source = item.product.imageUrl;

  if (!source || failedSource === source) {
    return (
      <div
        className={`${styles.imagePlaceholder} ${className ?? ''}`}
        role="img"
        aria-label={`${item.product.name} image unavailable`}
      >
        <span aria-hidden="true">◇</span>
      </div>
    );
  }

  return (
    <img
      className={className}
      src={source}
      alt={item.product.name}
      onError={() => setFailedSource(source)}
    />
  );
}

export function CartPage() {
  const navigate = useNavigate();
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const { items, loading, load, updateQuantity, remove, clear } = useCartStore();
  const [busyProduct, setBusyProduct] = useState<number | null>(null);
  const [confirmingClear, setConfirmingClear] = useState(false);
  const subtotal = cartTotal(items);

  useEffect(() => {
    void load();
  }, [load]);

  const update = async (productId: number, quantity: number) => {
    setBusyProduct(productId);
    try {
      await updateQuantity(productId, quantity);
    } catch (error) {
      toast.error(getErrorMessage(error, 'Unable to update this quantity.'));
    } finally {
      setBusyProduct(null);
    }
  };

  const removeItem = async (productId: number) => {
    setBusyProduct(productId);
    try {
      await remove(productId);
      toast.success('The item was removed from your cart.');
    } catch (error) {
      toast.error(getErrorMessage(error, 'Unable to remove this item.'));
    } finally {
      setBusyProduct(null);
    }
  };

  const clearCart = async () => {
    try {
      await clear();
      setConfirmingClear(false);
      toast.success('Your cart is now empty.');
    } catch (error) {
      toast.error(getErrorMessage(error, 'Unable to clear your cart.'));
    }
  };

  const proceedToCheckout = () => {
    if (!isAuthenticated) {
      toast.info('Sign in before continuing to secure checkout.');
      navigate('/login', { state: { returnTo: '/checkout' } });
      return;
    }
    navigate('/checkout');
  };

  return (
    <div className="container page">
      <header className="page-header">
        <div>
          <h1 className="page-title">Shopping cart</h1>
          <p className="page-subtitle">
            {items.length} product{items.length === 1 ? '' : 's'} ready for
            checkout
          </p>
        </div>
        {items.length > 0 ? (
          <button
            className="btn btn-outline-danger"
            type="button"
            onClick={() => setConfirmingClear(true)}
          >
            Clear cart
          </button>
        ) : null}
      </header>

      {loading ? (
        <Loader label="Loading your cart…" />
      ) : items.length === 0 ? (
        <div className="empty-state surface">
          <h2>Your cart is empty</h2>
          <p>Explore the catalogue and add something you’ll enjoy.</p>
          <Link className="btn btn-primary" to="/products">
            Browse products
          </Link>
        </div>
      ) : (
        <div className={styles.cartLayout}>
          <section className={styles.cartItems} aria-label="Cart items">
            {items.map((item) => {
              const productId = item.product.productId;
              const busy = busyProduct === productId;
              return (
                <article className={`${styles.cartItem} surface`} key={productId}>
                  <CartImage item={item} className={styles.cartImage} />
                  <div className={styles.cartItemDetails}>
                    <span className={styles.eyebrow}>
                      {item.product.categoryName || 'Product'}
                    </span>
                    <h2>
                      <Link to={`/products/${productId}`}>{item.product.name}</Link>
                    </h2>
                    <p>{formatCurrency(item.price)} each</p>
                    <button
                      className={styles.textDangerButton}
                      type="button"
                      disabled={busy}
                      onClick={() => void removeItem(productId)}
                    >
                      Remove
                    </button>
                  </div>
                  <div className={styles.quantityControl}>
                    <span>Quantity</span>
                    <div>
                      <button
                        type="button"
                        aria-label={`Decrease ${item.product.name} quantity`}
                        disabled={busy}
                        onClick={() => void update(productId, item.quantity - 1)}
                      >
                        −
                      </button>
                      <input
                        aria-label={`${item.product.name} quantity`}
                        type="number"
                        min="1"
                        max={Math.max(item.product.stock, 1)}
                        value={item.quantity}
                        disabled={busy}
                        onChange={(event) => {
                          const value = Number(event.target.value);
                          if (Number.isInteger(value) && value > 0) {
                            void update(productId, value);
                          }
                        }}
                      />
                      <button
                        type="button"
                        aria-label={`Increase ${item.product.name} quantity`}
                        disabled={
                          busy || item.quantity >= Math.max(item.product.stock, 1)
                        }
                        onClick={() => void update(productId, item.quantity + 1)}
                      >
                        +
                      </button>
                    </div>
                  </div>
                  <strong className={styles.linePrice}>
                    {formatCurrency(item.price * item.quantity)}
                  </strong>
                </article>
              );
            })}
          </section>

          <aside className={`${styles.orderSummary} surface`}>
            <h2>Order summary</h2>
            <dl>
              <div>
                <dt>Subtotal</dt>
                <dd>{formatCurrency(subtotal)}</dd>
              </div>
              <div>
                <dt>Tax</dt>
                <dd>Not added</dd>
              </div>
              <div>
                <dt>Shipping</dt>
                <dd>Free</dd>
              </div>
              <div className={styles.summaryTotal}>
                <dt>Total</dt>
                <dd>{formatCurrency(subtotal)}</dd>
              </div>
            </dl>
            <button
              className="btn btn-primary btn-lg w-100"
              type="button"
              onClick={proceedToCheckout}
            >
              Secure checkout
            </button>
            <p className={styles.secureNote}>
              Card details are entered securely through Stripe and are never
              stored by ShopSphere.
            </p>
            <div className={styles.checkoutCallout}>
              Standard shipping is free on all orders in supported regions.
            </div>
          </aside>
        </div>
      )}

      <Modal
        open={confirmingClear}
        title="Clear your cart?"
        size="sm"
        onClose={() => setConfirmingClear(false)}
        footer={
          <>
            <button
              className="btn btn-outline-secondary"
              type="button"
              onClick={() => setConfirmingClear(false)}
            >
              Keep items
            </button>
            <button
              className="btn btn-danger"
              type="button"
              onClick={() => void clearCart()}
            >
              Clear cart
            </button>
          </>
        }
      >
        This removes every item currently in your cart.
      </Modal>
    </div>
  );
}

function CheckoutForm({ items }: { items: CartItem[] }) {
  const stripe = useStripe();
  const elements = useElements();
  const navigate = useNavigate();
  const clearCart = useCartStore((state) => state.clear);
  const token = useAuthStore((state) => state.token);
  const [checkoutReference] = useState(() => crypto.randomUUID());
  const [paymentError, setPaymentError] = useState('');
  const [processingStep, setProcessingStep] = useState<
    'idle' | 'payment' | 'order'
  >('idle');
  const total = cartTotal(items);
  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors },
  } = useForm<CheckoutValues>({
    resolver: zodResolver(checkoutSchema),
    defaultValues: { fullName: '', address: '', phoneNumber: '' },
  });

  useEffect(() => {
    let cancelled = false;
    void authService
      .profile()
      .then((profile) => {
        if (!cancelled) {
          setValue(
            'fullName',
            `${profile.firstName} ${profile.lastName}`.trim(),
          );
        }
      })
      .catch(() => {
        // Checkout remains usable when profile prefill is unavailable.
      });
    return () => {
      cancelled = true;
    };
  }, [setValue]);

  const completeCart = async () => {
    try {
      await clearCart();
    } catch {
      const state = useCartStore.getState();
      writeJson(localStorage, state.storageKey, []);
      useCartStore.setState({ items: [] });
      toast.warning(
        'Your order is placed, but the server cart could not be cleared.',
      );
    }
  };

  const submit = async (values: CheckoutValues) => {
    setPaymentError('');
    if (!stripe || !elements) {
      setPaymentError('Secure payment is still initializing. Try again shortly.');
      return;
    }

    const card = elements.getElement(CardElement);
    if (!card) {
      setPaymentError('The secure card field is not ready.');
      return;
    }

    try {
      setProcessingStep('payment');
      const payment = await api.post<PaymentIntentResponse>(
        '/payments/create-payment-intent',
        {
          checkoutReference,
          items: items.map((item) => ({
            productId: item.product.productId,
            quantity: item.quantity,
          })),
        },
      );
      if (!payment?.clientSecret || !payment.paymentIntentId) {
        throw new Error(
          'The payment service did not return a secure payment reference.',
        );
      }

      const confirmation = await stripe.confirmCardPayment(
        payment.clientSecret,
        {
          payment_method: {
            card,
            billing_details: {
              name: values.fullName,
              phone: values.phoneNumber,
            },
          },
        },
      );
      if (confirmation.error) {
        throw new Error(
          confirmation.error.message || 'Stripe could not complete the payment.',
        );
      }
      if (confirmation.paymentIntent?.status !== 'succeeded') {
        throw new Error('Payment was not completed.');
      }
      if (confirmation.paymentIntent.id !== payment.paymentIntentId) {
        throw new Error('The confirmed payment reference did not match checkout.');
      }

      setProcessingStep('order');
      const payload: CheckoutRequest = {
        fullName: values.fullName,
        address: values.address,
        phoneNumber: values.phoneNumber,
        paymentIntentId: payment.paymentIntentId,
        items: items.map((item) => ({
          productId: item.product.productId,
          quantity: item.quantity,
        })),
      };
      const order = await api.post<CheckoutResponse>(
        '/orders/checkout',
        payload,
      );
      const orderId = String(order.orderId);
      const ownerSubject = getSubjectFromToken(token);
      const serverTotal = payment.amount / 100;
      const summary: LastOrderSummary = {
        ownerSubject: ownerSubject ?? '',
        orderId,
        fullName: values.fullName,
        address: values.address,
        phoneNumber: values.phoneNumber,
        items: items.map((item) => ({
          name: item.product.name,
          price: item.price,
          quantity: item.quantity,
          imageUrl: item.product.imageUrl,
        })),
        total: serverTotal,
      };
      localStorage.removeItem(storageKeys.lastOrderSummary);
      localStorage.removeItem(storageKeys.lastOrderItems);
      sessionStorage.removeItem(storageKeys.lastOrderSummary);
      sessionStorage.removeItem(storageKeys.lastOrderItems);
      if (ownerSubject) {
        writeJson(
          sessionStorage,
          `${storageKeys.lastOrderSummary}:${ownerSubject}`,
          summary,
        );
        writeJson(
          sessionStorage,
          `${storageKeys.lastOrderItems}:${ownerSubject}`,
          summary.items,
        );
      }
      await completeCart();
      navigate(`/success?orderId=${encodeURIComponent(orderId)}`, {
        replace: true,
      });
    } catch (error) {
      setPaymentError(
        getErrorMessage(error, 'Checkout failed. Your order was not placed.'),
      );
      setProcessingStep('idle');
    }
  };

  const submitting = processingStep !== 'idle';
  const stripeConfigured = Boolean(environment.stripePublishableKey);
  const activeStep = processingStep === 'payment' ? 2 : processingStep === 'order' ? 3 : 1;
  const progressMessage =
    processingStep === 'payment'
      ? 'Payment is being authorized'
      : processingStep === 'order'
        ? 'Placing your order'
        : 'Review details and complete checkout';

  return (
    <form className={styles.checkoutForm} onSubmit={handleSubmit(submit)} noValidate>
      <CheckoutProgress activeStep={activeStep} message={progressMessage} />
      <section className={`${styles.checkoutPanel} surface`}>
        <span className={styles.stepLabel}>Step 1 of 2</span>
        <h2>Delivery details</h2>
        <label className={styles.formField} htmlFor="checkout-name">
          <span>Full name</span>
          <input
            {...register('fullName')}
            id="checkout-name"
            className={`form-control ${errors.fullName ? 'is-invalid' : ''}`}
            autoComplete="name"
          />
          {errors.fullName ? (
            <small className={styles.fieldError}>{errors.fullName.message}</small>
          ) : null}
        </label>
        <label className={styles.formField} htmlFor="checkout-address">
          <span>Shipping address</span>
          <textarea
            {...register('address')}
            id="checkout-address"
            className={`form-control ${errors.address ? 'is-invalid' : ''}`}
            rows={4}
            autoComplete="street-address"
          />
          {errors.address ? (
            <small className={styles.fieldError}>{errors.address.message}</small>
          ) : null}
        </label>
        <label className={styles.formField} htmlFor="checkout-phone">
          <span>Phone number</span>
          <input
            {...register('phoneNumber')}
            id="checkout-phone"
            className={`form-control ${errors.phoneNumber ? 'is-invalid' : ''}`}
            type="tel"
            inputMode="tel"
            autoComplete="tel"
          />
          {errors.phoneNumber ? (
            <small className={styles.fieldError}>
              {errors.phoneNumber.message}
            </small>
          ) : null}
        </label>
      </section>

      <section className={`${styles.checkoutPanel} surface`}>
        <span className={styles.stepLabel}>Step 2 of 2</span>
        <h2>Secure payment</h2>
        {!stripeConfigured ? (
          <div className="alert alert-warning" role="alert">
            Stripe is not configured. Set <code>VITE_STRIPE_PUBLISHABLE_KEY</code>{' '}
            before accepting payments.
          </div>
        ) : (
          <div className={styles.stripeField}>
            <CardElement
              options={{
                hidePostalCode: false,
                style: {
                  base: {
                    color: '#1e293b',
                    fontFamily: 'Inter, system-ui, sans-serif',
                    fontSize: '16px',
                    '::placeholder': { color: '#94a3b8' },
                  },
                  invalid: { color: '#dc2626' },
                },
              }}
            />
          </div>
        )}
        <p className={styles.secureNote}>
          Stripe securely hosts this card field. ShopSphere cannot read or store
          the card number, expiry, or security code.
        </p>
      </section>

      {paymentError ? (
        <div className="alert alert-danger" role="alert">
          {paymentError}
        </div>
      ) : null}

      <button
        className="btn btn-primary btn-lg w-100"
        type="submit"
        disabled={submitting || !stripeConfigured || !stripe}
      >
        {processingStep === 'payment'
          ? 'Authorizing payment…'
          : processingStep === 'order'
            ? 'Placing order…'
            : `Pay ${formatCurrency(total)}`}
      </button>
      <p className={styles.checkoutCallout}>
        We never store your card details. Stripe handles all payment security.
      </p>
    </form>
  );
}

export function CheckoutPage() {
  const location = useLocation();
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const items = useCartStore((state) => state.items);
  const total = useMemo(() => cartTotal(items), [items]);

  if (!isAuthenticated) {
    return (
      <Navigate
        to="/login"
        replace
        state={{ returnTo: `${location.pathname}${location.search}` }}
      />
    );
  }

  if (items.length === 0) {
    return (
      <div className="container page">
        <div className="empty-state surface">
          <h1>Your cart is empty</h1>
          <p>Add at least one item before starting checkout.</p>
          <Link className="btn btn-primary" to="/products">
            Browse products
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="container page">
      <header className="page-header">
        <div>
          <Link className={styles.backLink} to="/cart">
            ← Back to cart
          </Link>
          <h1 className="page-title">Secure checkout</h1>
          <p className="page-subtitle">
            Review delivery details and authorize your payment.
          </p>
        </div>
      </header>
      <div className={styles.checkoutLayout}>
        <Elements stripe={stripePromise}>
          <CheckoutForm items={items} />
        </Elements>
        <aside className={`${styles.orderSummary} surface`}>
          <h2>Your order</h2>
          <div className={styles.checkoutItems}>
            {items.map((item) => (
              <div key={item.product.productId}>
                <span>
                  {item.product.name} × {item.quantity}
                </span>
                <strong>{formatCurrency(item.price * item.quantity)}</strong>
              </div>
            ))}
          </div>
          <dl>
            <div className={styles.summaryTotal}>
              <dt>Total charged</dt>
              <dd>{formatCurrency(total)}</dd>
            </div>
          </dl>
          <p className={styles.summaryFootnote}>
            Tax and shipping are not currently added by the order API.
          </p>
        </aside>
      </div>
    </div>
  );
}

export function SuccessPage() {
  const [searchParams] = useSearchParams();
  const token = useAuthStore((state) => state.token);
  const queryOrderId = searchParams.get('orderId');
  const ownerSubject = getSubjectFromToken(token);
  localStorage.removeItem(storageKeys.lastOrderSummary);
  localStorage.removeItem(storageKeys.lastOrderItems);
  sessionStorage.removeItem(storageKeys.lastOrderSummary);
  sessionStorage.removeItem(storageKeys.lastOrderItems);
  const summaryKey = ownerSubject
    ? `${storageKeys.lastOrderSummary}:${ownerSubject}`
    : null;
  const storedSummary = summaryKey
    ? readJson<LastOrderSummary | null>(sessionStorage, summaryKey, null)
    : null;
  const summary =
    storedSummary?.ownerSubject === ownerSubject ? storedSummary : null;
  if (summaryKey && storedSummary && !summary) {
    sessionStorage.removeItem(summaryKey);
    sessionStorage.removeItem(
      `${storageKeys.lastOrderItems}:${ownerSubject}`,
    );
  }
  const orderId = queryOrderId || summary?.orderId;

  return (
    <div className={`${styles.successPage} container page`}>
      <section className={`${styles.successCard} surface`}>
        <div className={styles.successMark} aria-hidden="true">
          ✓
        </div>
        <span className={styles.eyebrow}>Order confirmed</span>
        <h1>Thank you for your order.</h1>
        <p>
          Your payment was successful and the order has been sent for
          fulfillment.
        </p>
        {orderId ? (
          <div className={styles.orderNumber}>
            <span>Order ID</span>
            <strong>{orderId}</strong>
          </div>
        ) : null}

        {summary ? (
          <div className={styles.successSummary}>
            <h2>Order summary</h2>
            {summary.items.map((item, index) => (
              <div key={`${item.name}-${index}`}>
                <span>
                  {item.name} × {item.quantity}
                </span>
                <strong>{formatCurrency(item.price * item.quantity)}</strong>
              </div>
            ))}
            <div className={styles.successTotal}>
              <span>Total paid</span>
              <strong>{formatCurrency(summary.total)}</strong>
            </div>
            <p>
              Delivering to <strong>{summary.fullName}</strong>,{' '}
              {summary.address}
            </p>
          </div>
        ) : null}

        <div className={styles.successActions}>
          <Link className="btn btn-primary" to="/my-orders">
            View my orders
          </Link>
          <Link className="btn btn-outline-primary" to="/products">
            Continue shopping
          </Link>
        </div>
      </section>
    </div>
  );
}
