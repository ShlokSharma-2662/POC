import { expect, test } from '@playwright/test';

test.beforeEach(async ({ page }) => {
  await page.route('**/api/categories', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        isSuccessful: true,
        status: 'Success',
        data: [],
      }),
    }),
  );
});

test('renders the storefront shell', async ({ page }) => {
  await page.goto('/home');
  await expect(page.getByRole('link', { name: /ShopSphere/i })).toBeVisible();
  await expect(page.getByRole('navigation', { name: /main/i })).toBeVisible();
  await expect(page).toHaveURL(/\/home$/);
  await expect
    .poll(() =>
      page.evaluate(
        () =>
          document.documentElement.scrollWidth <=
          document.documentElement.clientWidth,
      ),
    )
    .toBe(true);
});

test('protects account routes', async ({ page }) => {
  await page.goto('/profile');
  await expect(page).toHaveURL(/\/login$/);
  await expect(
    page.getByRole('heading', { name: /welcome back/i }),
  ).toBeVisible();
});

test('falls back to the home route', async ({ page }) => {
  await page.goto('/route-that-does-not-exist');
  await expect(page).toHaveURL(/\/home$/);
});
