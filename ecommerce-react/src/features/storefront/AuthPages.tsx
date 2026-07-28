import { useEffect, useState, type ReactNode } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import {
  Link,
  useLocation,
  useNavigate,
  useSearchParams,
} from 'react-router-dom';
import { toast } from 'sonner';
import { z } from 'zod';
import { Loader } from '../../components/ui/Loader';
import { Modal } from '../../components/ui/Modal';
import {
  authService,
  type OAuthExchangeResponse,
  type OAuthProvider,
} from '../../services/auth-service';
import { useAuthStore } from '../../stores/auth-store';
import styles from './Storefront.module.scss';
import { getErrorMessage } from './storefront-utils';

const loginSchema = z.object({
  email: z.email('Enter a valid email address.'),
  password: z.string().min(1, 'Password is required.'),
});

const registerSchema = z.object({
  firstName: z
    .string()
    .trim()
    .min(1, 'First name is required.')
    .max(50, 'First name cannot exceed 50 characters.'),
  lastName: z
    .string()
    .trim()
    .min(1, 'Last name is required.')
    .max(50, 'Last name cannot exceed 50 characters.'),
  email: z.email('Enter a valid email address.'),
  password: z
    .string()
    .min(8, 'Use at least 8 characters.')
    .regex(/[A-Z]/, 'Add at least one uppercase letter.')
    .regex(/[a-z]/, 'Add at least one lowercase letter.')
    .regex(/[0-9]/, 'Add at least one number.')
    .regex(/[^a-zA-Z0-9]/, 'Add at least one special character.'),
});

type LoginValues = z.infer<typeof loginSchema>;
type RegisterValues = z.infer<typeof registerSchema>;

function safeReturnTo(value: unknown): string | null {
  return typeof value === 'string' &&
    value.startsWith('/') &&
    !value.startsWith('//')
    ? value
    : null;
}

function destinationFor(role: string | null | undefined, returnTo?: unknown) {
  const requested = safeReturnTo(returnTo);
  if (requested) return requested;
  return role?.toLowerCase() === 'admin' ? '/admin' : '/home';
}

const pendingOAuthExchanges = new Map<
  string,
  Promise<OAuthExchangeResponse>
>();

function exchangeOAuthCodeOnce(provider: OAuthProvider, code: string) {
  const key = `${provider}:${code}`;
  const existing = pendingOAuthExchanges.get(key);
  if (existing) return existing;

  const request = authService.exchangeOAuthCode(provider, code);
  pendingOAuthExchanges.set(key, request);
  const clearRequest = () => {
    window.setTimeout(() => pendingOAuthExchanges.delete(key), 60_000);
  };
  void request.then(clearRequest, clearRequest);
  return request;
}

function AuthCard({
  title,
  subtitle,
  children,
}: {
  title: string;
  subtitle: string;
  children: ReactNode;
}) {
  return (
    <div className={styles.authPage}>
      <section className={styles.authAside}>
        <Link className={styles.authBrand} to="/home">
          ShopSphere
        </Link>
        <div>
          <span className={styles.heroKicker}>Shop with confidence</span>
          <h1>Everything you saved, ordered, and loved—in one place.</h1>
          <p>
            Sign in to sync your cart, check order progress, and keep your
            account secure.
          </p>
        </div>
      </section>
      <main className={styles.authMain}>
        <div className={styles.authCard}>
          <div className={styles.authHeading}>
            <h1>{title}</h1>
            <p>{subtitle}</p>
          </div>
          {children}
        </div>
      </main>
    </div>
  );
}

function PasswordInput({
  id,
  label,
  error,
  registration,
  autoComplete,
}: {
  id: string;
  label: string;
  error?: string;
  registration: ReturnType<ReturnType<typeof useForm<LoginValues>>['register']>;
  autoComplete: string;
}) {
  const [visible, setVisible] = useState(false);

  return (
    <label className={styles.formField} htmlFor={id}>
      <span>{label}</span>
      <div className={styles.passwordField}>
        <input
          {...registration}
          id={id}
          className={`form-control ${error ? 'is-invalid' : ''}`}
          type={visible ? 'text' : 'password'}
          autoComplete={autoComplete}
          aria-invalid={Boolean(error)}
          aria-describedby={error ? `${id}-error` : undefined}
        />
        <button
          type="button"
          onClick={() => setVisible((value) => !value)}
          aria-label={`${visible ? 'Hide' : 'Show'} ${label.toLowerCase()}`}
        >
          {visible ? 'Hide' : 'Show'}
        </button>
      </div>
      {error ? (
        <small className={styles.fieldError} id={`${id}-error`}>
          {error}
        </small>
      ) : null}
    </label>
  );
}

export function LoginPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const setToken = useAuthStore((state) => state.setToken);
  const [serverError, setServerError] = useState('');
  const [forgotPasswordOpen, setForgotPasswordOpen] = useState(false);
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: '', password: '' },
  });
  const returnTo = (location.state as { returnTo?: unknown } | null)?.returnTo;

  const submit = async (values: LoginValues) => {
    setServerError('');
    try {
      const response = await authService.login(values);
      setToken(response.token, response);
      toast.success(`Welcome back${response.firstName ? `, ${response.firstName}` : ''}.`);
      navigate(destinationFor(response.role, returnTo), { replace: true });
    } catch (error) {
      setServerError(
        getErrorMessage(error, 'Login failed. Check your email and password.'),
      );
    }
  };

  const startOAuth = (provider: 'microsoft' | 'google') => {
    const callbackUrl = `${window.location.origin}/oauth/callback`;
    const destination = safeReturnTo(returnTo);
    const url =
      provider === 'microsoft'
        ? authService.microsoftLoginUrl(callbackUrl, destination)
        : authService.googleLoginUrl(callbackUrl, destination);
    window.location.assign(url);
  };

  return (
    <AuthCard title="Welcome back" subtitle="Sign in to continue to your account.">
      {serverError ? (
        <div className="alert alert-danger" role="alert">
          {serverError}
        </div>
      ) : null}
      <form className={styles.authForm} onSubmit={handleSubmit(submit)} noValidate>
        <label className={styles.formField} htmlFor="login-email">
          <span>Email address</span>
          <input
            {...register('email')}
            id="login-email"
            className={`form-control ${errors.email ? 'is-invalid' : ''}`}
            type="email"
            autoComplete="email"
            autoFocus
            aria-invalid={Boolean(errors.email)}
          />
          {errors.email ? (
            <small className={styles.fieldError}>{errors.email.message}</small>
          ) : null}
        </label>
        <PasswordInput
          id="login-password"
          label="Password"
          autoComplete="current-password"
          registration={register('password')}
          error={errors.password?.message}
        />
        <div className={styles.formUtilityRow}>
          <button
            className={styles.textButton}
            type="button"
            onClick={() => setForgotPasswordOpen(true)}
          >
            Forgot password?
          </button>
        </div>
        <button className="btn btn-primary btn-lg" type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Signing in…' : 'Sign in'}
        </button>
      </form>

      <div className={styles.divider}>
        <span>or continue with</span>
      </div>
      <div className={styles.oauthButtons}>
        <button
          className="btn btn-outline-secondary"
          type="button"
          disabled={isSubmitting}
          onClick={() => startOAuth('microsoft')}
        >
          Microsoft
        </button>
        <button
          className="btn btn-outline-secondary"
          type="button"
          disabled={isSubmitting}
          onClick={() => startOAuth('google')}
        >
          Google
        </button>
      </div>
      <p className={styles.authSwitch}>
        New to ShopSphere? <Link to="/register">Create an account</Link>
      </p>

      <Modal
        open={forgotPasswordOpen}
        title="Need help signing in?"
        size="sm"
        onClose={() => setForgotPasswordOpen(false)}
        footer={
          <button
            className="btn btn-primary"
            type="button"
            onClick={() => setForgotPasswordOpen(false)}
          >
            Got it
          </button>
        }
      >
        Password reset is not yet available in the API. Contact{' '}
        <a href="mailto:support@eshop.com">support@eshop.com</a> for account
        assistance.
      </Modal>
    </AuthCard>
  );
}

export function RegisterPage() {
  const navigate = useNavigate();
  const [serverError, setServerError] = useState('');
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<RegisterValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: {
      firstName: '',
      lastName: '',
      email: '',
      password: '',
    },
  });

  const submit = async (values: RegisterValues) => {
    setServerError('');
    try {
      await authService.register(values);
      toast.success('Your account was created. You can now sign in.');
      navigate('/login', { replace: true });
    } catch (error) {
      setServerError(
        getErrorMessage(error, 'Registration failed. Please try again.'),
      );
    }
  };

  const startGoogleLogin = () => {
    const callbackUrl = `${window.location.origin}/oauth/callback`;
    window.location.assign(authService.googleLoginUrl(callbackUrl));
  };

  return (
    <AuthCard
      title="Create your account"
      subtitle="A few details are all it takes to get started."
    >
      {serverError ? (
        <div className="alert alert-danger" role="alert">
          {serverError}
        </div>
      ) : null}
      <form className={styles.authForm} onSubmit={handleSubmit(submit)} noValidate>
        <div className={styles.twoColumnFields}>
          <label className={styles.formField} htmlFor="first-name">
            <span>First name</span>
            <input
              {...register('firstName')}
              id="first-name"
              className={`form-control ${errors.firstName ? 'is-invalid' : ''}`}
              autoComplete="given-name"
              autoFocus
            />
            {errors.firstName ? (
              <small className={styles.fieldError}>
                {errors.firstName.message}
              </small>
            ) : null}
          </label>
          <label className={styles.formField} htmlFor="last-name">
            <span>Last name</span>
            <input
              {...register('lastName')}
              id="last-name"
              className={`form-control ${errors.lastName ? 'is-invalid' : ''}`}
              autoComplete="family-name"
            />
            {errors.lastName ? (
              <small className={styles.fieldError}>{errors.lastName.message}</small>
            ) : null}
          </label>
        </div>
        <label className={styles.formField} htmlFor="register-email">
          <span>Email address</span>
          <input
            {...register('email')}
            id="register-email"
            className={`form-control ${errors.email ? 'is-invalid' : ''}`}
            type="email"
            autoComplete="email"
          />
          {errors.email ? (
            <small className={styles.fieldError}>{errors.email.message}</small>
          ) : null}
        </label>
        <label className={styles.formField} htmlFor="register-password">
          <span>Password</span>
          <input
            {...register('password')}
            id="register-password"
            className={`form-control ${errors.password ? 'is-invalid' : ''}`}
            type="password"
            autoComplete="new-password"
          />
          <small className={errors.password ? styles.fieldError : styles.fieldHint}>
            {errors.password?.message ??
              'Use 8+ characters with upper/lowercase, a number, and a symbol.'}
          </small>
        </label>
        <button className="btn btn-primary btn-lg" type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Creating account…' : 'Create account'}
        </button>
      </form>
      <div className={styles.divider}>
        <span>or</span>
      </div>
      <button
        className="btn btn-outline-secondary w-100"
        type="button"
        disabled={isSubmitting}
        onClick={startGoogleLogin}
      >
        Continue with Google
      </button>
      <p className={styles.authSwitch}>
        Already have an account? <Link to="/login">Sign in</Link>
      </p>
    </AuthCard>
  );
}

export function OAuthCallbackPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const setToken = useAuthStore((state) => state.setToken);
  const [state, setState] = useState<
    | { status: 'loading'; message: string }
    | { status: 'success'; message: string }
    | { status: 'error'; message: string }
  >({ status: 'loading', message: 'Completing authentication…' });

  useEffect(() => {
    let cancelled = false;
    let redirectTimer: number | undefined;

    const complete = async () => {
      const oauthError = searchParams.get('error');
      if (oauthError) {
        setState({
          status: 'error',
          message: `OAuth authentication failed: ${oauthError}`,
        });
        return;
      }

      const code = searchParams.get('code');
      const provider = searchParams.get('provider');
      if (!code || (provider !== 'microsoft' && provider !== 'google')) {
        setState({
          status: 'error',
          message: 'The OAuth completion response is missing or invalid.',
        });
        return;
      }

      try {
        window.history.replaceState({}, document.title, window.location.pathname);
        const response = await exchangeOAuthCodeOnce(provider, code);

        if (cancelled) return;
        if (!response.token || !response.email) {
          throw new Error('We could not retrieve your account information.');
        }
        setToken(response.token, response);

        setState({
          status: 'success',
          message: 'Authentication complete. Redirecting…',
        });
        redirectTimer = window.setTimeout(() => {
          navigate(destinationFor(response.role, response.returnTo), {
            replace: true,
          });
        }, 700);
      } catch (error) {
        if (!cancelled) {
          setState({
            status: 'error',
            message: getErrorMessage(
              error,
              'Failed to retrieve your account information.',
            ),
          });
        }
      }
    };

    void complete();
    return () => {
      cancelled = true;
      if (redirectTimer !== undefined) window.clearTimeout(redirectTimer);
    };
  }, [navigate, searchParams, setToken]);

  return (
    <div className={`${styles.callbackPage} container`}>
      <section className={`${styles.callbackCard} surface`} aria-live="polite">
        {state.status === 'loading' ? (
          <Loader label={state.message} />
        ) : state.status === 'success' ? (
          <>
            <div className={styles.successMark} aria-hidden="true">
              ✓
            </div>
            <h1>Authentication successful</h1>
            <p>{state.message}</p>
          </>
        ) : (
          <>
            <div className={styles.errorMark} aria-hidden="true">
              !
            </div>
            <h1>Authentication failed</h1>
            <p role="alert">{state.message}</p>
            <Link className="btn btn-primary" to="/login">
              Try again
            </Link>
          </>
        )}
      </section>
    </div>
  );
}
