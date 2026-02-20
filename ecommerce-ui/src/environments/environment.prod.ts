// export const environment = {
//   production: true,
//   apiUrl: 'http://bulkybook-fn-dev.rysun.in:9090/api',
//   stripePublishableKey: 'pk_test_51RyrEsRq2tUM97cWNi5vHV0HERcfOx7TOvWEQPxtakvRyqtvHswDcvYBi6EfnAchG6sGdz7KaGLTYb9gVerr1aZa00ccg6zAQ3',
//   oauth: {
//     enabled: true,
//     provider: 'microsoft',
//     clientId: '45318ea1-6cc3-418a-90ed-a515b06d30da',
//     authority: 'https://login.microsoftonline.com/21bb67cf-16db-4f8f-a738-5c8de0726b81/v2.0',
//     scope: 'openid profile email',
//     redirectUri: 'https://your-domain.com/oauth/callback' // Update this with your actual domain
//   },
//   rateLimiting: {
//     enabled: true,
//     showHeaders: true,
//     retryAfterSeconds: 60
//   }
// };

export const environment = {
  production: true,

  // ✅ Use your live backend API base URL (include trailing slash)
  apiUrl: 'http://bulkybook-fn-dev.rysun.in:9090/api',

  // ✅ Stripe publishable key (keep as provided)
  stripePublishableKey: 'pk_test_51RyrEsRq2tUM97cWNi5vHV0HERcfOx7TOvWEQPxtakvRyqtvHswDcvYBi6EfnAchG6sGdz7KaGLTYb9gVerr1aZa00ccg6zAQ3',

  // ✅ OAuth configuration — replace redirectUri with your actual frontend domain
  oauth: {
    enabled: true,
    provider: 'microsoft',
    clientId: '45318ea1-6cc3-418a-90ed-a515b06d30da',
    authority: 'https://login.microsoftonline.com/21bb67cf-16db-4f8f-a738-5c8de0726b81/v2.0',
    scope: 'openid profile email',
    redirectUri: 'http://bulkybookui.rysun.in:9090/oauth/callback'
  },

  // ✅ Optional rate-limiting feature flags
  rateLimiting: {
    enabled: true,
    showHeaders: true,
    retryAfterSeconds: 60
  }
};
