# Ecommerce React frontend

This is the React replacement for the Angular application in `../ecommerce-ui`.
The applications remain side by side during migration so routes can be verified
before the production cutover.

## Local development

1. Copy `.env.example` to `.env.local`.
2. Set `VITE_STRIPE_PUBLISHABLE_KEY` to the Stripe publishable test key.
3. Start the ASP.NET services.
4. Run:

   ```powershell
   npm ci
   npm run dev
   ```

The Vite server listens on `http://localhost:4200`. It proxies `/api/orders` to
`VITE_DEV_ORDER_API_TARGET` (`https://localhost:64385` by default), and other
`/api` plus `/images` requests to `VITE_DEV_API_TARGET`
(`https://localhost:7273` by default).

## Quality checks

```powershell
npm run typecheck
npm run lint
npm test
npm run build
npm run e2e
```

## Architecture

- React Router owns the existing Angular-compatible URL map.
- TanStack Query manages API-backed server state.
- Zustand stores preserve the Angular cart, wishlist, and authentication storage
  keys during the transition.
- `src/lib/api-client.ts` attaches the bearer token, unwraps the backend
  `ApiResponse<T>` envelope, and centralizes 401/403/429 behavior.
- SCSS modules replace Angular's emulated component style encapsulation.

Only variables prefixed with `VITE_` are available in browser code. They are
public at build time and must never contain backend secrets.
