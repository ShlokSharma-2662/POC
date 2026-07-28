import {
  createBrowserRouter,
  Navigate,
  Outlet,
} from 'react-router-dom';
import { AuthLifecycle } from './components/auth/AuthLifecycle';
import { ProtectedRoute } from './components/auth/ProtectedRoute';
import { AdminShell } from './components/layout/AdminShell';
import { AppShell } from './components/layout/AppShell';
import { Loader } from './components/ui/Loader';
import { RouteErrorBoundary } from './components/ui/RouteErrorBoundary';

export const router = createBrowserRouter([
  {
    element: (
      <AuthLifecycle>
        <Outlet />
      </AuthLifecycle>
    ),
    errorElement: <RouteErrorBoundary />,
    hydrateFallbackElement: <Loader fullPage label="Loading ShopSphere…" />,
    children: [
      {
        element: <AppShell />,
        children: [
          { index: true, element: <Navigate to="/home" replace /> },
          {
            path: 'home',
            lazy: async () => ({
              Component: (await import('./features/storefront')).HomePage,
            }),
          },
          {
            path: 'products',
            lazy: async () => ({
              Component: (await import('./features/storefront')).ProductsPage,
            }),
          },
          {
            path: 'products/:productId',
            lazy: async () => ({
              Component: (await import('./features/storefront'))
                .ProductDetailPage,
            }),
          },
          {
            path: 'wishlist',
            lazy: async () => ({
              Component: (await import('./features/storefront')).WishlistPage,
            }),
          },
          {
            path: 'cart',
            lazy: async () => ({
              Component: (await import('./features/storefront')).CartPage,
            }),
          },
          {
            path: 'login',
            lazy: async () => ({
              Component: (await import('./features/storefront')).LoginPage,
            }),
          },
          {
            path: 'register',
            lazy: async () => ({
              Component: (await import('./features/storefront')).RegisterPage,
            }),
          },
          {
            path: 'oauth/callback',
            lazy: async () => ({
              Component: (await import('./features/storefront'))
                .OAuthCallbackPage,
            }),
          },
          {
            path: 'unauthorized',
            lazy: async () => ({
              Component: (await import('./features/storefront'))
                .UnauthorizedPage,
            }),
          },
          {
            element: <ProtectedRoute />,
            children: [
              {
                path: 'checkout',
                lazy: async () => ({
                  Component: (await import('./features/storefront'))
                    .CheckoutPage,
                }),
              },
              {
                path: 'success',
                lazy: async () => ({
                  Component: (await import('./features/storefront'))
                    .SuccessPage,
                }),
              },
              {
                path: 'my-orders',
                lazy: async () => ({
                  Component: (await import('./features/storefront'))
                    .OrdersPage,
                }),
              },
              {
                path: 'profile',
                lazy: async () => ({
                  Component: (await import('./features/storefront'))
                    .ProfilePage,
                }),
              },
              {
                path: 'change-password',
                lazy: async () => ({
                  Component: (await import('./features/storefront'))
                    .ChangePasswordPage,
                }),
              },
            ],
          },
          { path: '*', element: <Navigate to="/home" replace /> },
        ],
      },
      {
        element: <ProtectedRoute adminOnly />,
        children: [
          {
            path: 'admin',
            element: <AdminShell />,
            children: [
              {
                index: true,
                lazy: async () => ({
                  Component: (await import('./features/admin'))
                    .AdminDashboardPage,
                }),
              },
              {
                path: 'orders',
                lazy: async () => ({
                  Component: (await import('./features/admin')).AdminOrdersPage,
                }),
              },
              {
                path: 'users',
                lazy: async () => ({
                  Component: (await import('./features/admin')).AdminUsersPage,
                }),
              },
              {
                path: 'products',
                lazy: async () => ({
                  Component: (await import('./features/admin')).AdminProductsPage,
                }),
              },
              {
                path: 'performance',
                lazy: async () => ({
                  Component: (await import('./features/admin'))
                    .PerformanceDashboardPage,
                }),
              },
              {
                path: 'errors',
                lazy: async () => ({
                  Component: (await import('./features/admin')).ErrorLogsPage,
                }),
              },
              {
                path: 'revenue',
                lazy: async () => ({
                  Component: (await import('./features/admin')).RevenuePage,
                }),
              },
            ],
          },
        ],
      },
    ],
  },
]);
