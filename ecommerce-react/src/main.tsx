import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { RouterProvider } from 'react-router-dom';
import { Toaster } from 'sonner';
import { router } from './router';
import 'bootstrap/dist/css/bootstrap.min.css';
import './styles/global.scss';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: (failureCount, error) => {
        const status =
          typeof error === 'object' && error && 'status' in error
            ? Number(error.status)
            : 0;
        return status >= 400 && status < 500 ? false : failureCount < 2;
      },
    },
  },
});

const root = document.getElementById('root');
if (!root) throw new Error('Root element was not found.');

createRoot(root).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
      <Toaster richColors position="top-right" closeButton />
    </QueryClientProvider>
  </StrictMode>,
);
