# React frontend migration

## Strategy

The React frontend is deployed from `ecommerce-react/`. The Angular application
remains in `ecommerce-ui/` as a rollback artifact, avoiding a framework mix in
the same browser document.

React keeps the existing public URLs, API response envelope, bearer-token keys,
cart keys, and wishlist key. OAuth now returns a short-lived, single-use code to
`/oauth/callback`; the callback exchanges it with the API so a JWT is never put
in the browser URL. Two routes are made explicit during the migration:

- `/products/:productId` fixes the Angular wishlist deep-link mismatch.
- `/checkout` hosts the Stripe Elements checkout that was present but unrouted
  in Angular.

## Route ownership

| Area | Routes |
| --- | --- |
| Public | `/home`, `/products`, `/products/:productId`, `/cart`, `/wishlist` |
| Authentication | `/login`, `/register`, `/oauth/callback`, `/unauthorized` |
| Account | `/profile`, `/change-password`, `/my-orders`, `/success` |
| Checkout | `/checkout` |
| Administration | `/admin`, `/admin/orders`, `/admin/users`, `/admin/products`, `/admin/performance`, `/admin/errors`, `/admin/revenue` |

## Compatibility contracts

- Production API base: `/api`
- Product images: `/images`
- JWT keys: `authToken`, `accessToken`
- Guest cart key: `cart_guest`
- Authenticated cart key: `cart_{subject}`
- Guest wishlist key: `guest_wishlist`
- Authenticated wishlist key: `wishlist_{subject}`
- Last-order key: `lastOrderSummary_{subject}` in session storage

Authentication lifecycle handling scopes server queries and browser state to the
JWT subject. Guest cart and wishlist data is merged once after login, with
deduplication and stock caps, and is cleared only after a successful merge.

## Release gates

- [x] TypeScript, lint, unit tests, and production build pass.
- [x] Docker Compose configuration validates for the default and legacy
      profiles.
- [ ] Docker Compose images build and the full stack starts on a machine with a
      running Docker daemon.
- [ ] Password login and both OAuth providers complete on the public origin.
- [x] Anonymous/authenticated cart and wishlist isolation, merge, and stale
      response behavior pass automated tests.
- [ ] Stripe creates and confirms a PaymentIntent without card details entering
      application state.
- [ ] Checkout creates an order and `/my-orders` can retrieve it.
- [x] User/admin guards reject invalid and expired tokens.
- [ ] Product images display in local development and through production Nginx.
- [ ] Admin user, product, order, metrics, error, and revenue operations pass.
- [x] Deep-link refresh is handled by the production Nginx fallback.
- [x] Playwright smoke journeys pass on desktop and mobile viewports.

## Production prerequisites

1. Apply
   `EcommerceAPI/BulkyBook-POC/Ecommerce.Infrastructure/Persistence/Migrations/20260728_secure_checkout.sql`
   to every existing database before enabling checkout. The application still
   uses `EnsureCreated`, so this migration is intentionally an explicit rollout
   step.
2. Set `SQL_SA_PASSWORD`, `JWT_SECRET_KEY`, `STRIPE_SECRET_KEY`,
   `STRIPE_PUBLISHABLE_KEY`, `FRONTEND_BASE_URL`, and `PUBLIC_APP_BASE_URL`
   from a secret manager or deployment environment. Do not use repository
   defaults for production.
3. Register each provider callback at
   `{PUBLIC_APP_BASE_URL}/api/oauth/callback` (Microsoft) and
   `{PUBLIC_APP_BASE_URL}/api/googleoauth/callback` (Google).
4. Keep Redis available for OAuth authorization grants when more than one API
   replica is running. The in-memory fallback is safe only within one process.
5. Run the unchecked release gates above with real provider and Stripe test-mode
   credentials before directing production traffic.

Checkout amounts are priced by the API from active products. PaymentIntent
metadata binds the authenticated user and a cart fingerprint; order creation
validates the successful payment again, decrements stock in a serializable
transaction, and prevents PaymentIntent replay. Current amount conversion
supports two-decimal currencies. A durable Stripe webhook/outbox flow is the
recommended next hardening step.

## Cutover

The `ecommerce-ui` Compose service now builds `ecommerce-react/` while retaining
the service name and public port 4200. The Angular rollback image is available
at port 4201 with `docker compose --profile legacy up -d ecommerce-angular`.
Promoting that image to port 4200 does not require a backend rollback.
