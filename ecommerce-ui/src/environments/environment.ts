export const environment = {
  production: false,
  apiUrl: 'https://localhost:7273/api',
  stripePublishableKey: 'pk_test_51RyrEsRq2tUM97cWNi5vHV0HERcfOx7TOvWEQPxtakvRyqtvHswDcvYBi6EfnAchG6sGdz7KaGLTYb9gVerr1aZa00ccg6zAQ3',
  oauth: {
    enabled: true,
    provider: 'microsoft',
    clientId: '45318ea1-6cc3-418a-90ed-a515b06d30da',
    authority: 'https://login.microsoftonline.com/21bb67cf-16db-4f8f-a738-5c8de0726b81/v2.0',
    scope: 'openid profile email',
    redirectUri: 'https://localhost:4200/oauth/callback'
  },
  rateLimiting: {
    enabled: true,
    showHeaders: true,
    retryAfterSeconds: 60
  }
};
