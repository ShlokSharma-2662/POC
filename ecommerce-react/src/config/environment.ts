export const environment = {
  apiBaseUrl: (import.meta.env.VITE_API_BASE_URL || '/api').replace(/\/$/, ''),
  stripePublishableKey: import.meta.env.VITE_STRIPE_PUBLISHABLE_KEY || '',
} as const;
