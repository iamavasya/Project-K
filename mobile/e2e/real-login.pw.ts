import { expect, test } from '@playwright/test';

/**
 * Sign-in against the real API: the e2e stack (scripts/dev.sh up e2e) serves the web image with the
 * PWA under /m/ and the API next to it, seeded with the web suite's accounts. Runs only when
 * PW_REAL_API points at that API (and E2E_MEMBER_PASSWORD is set), so the plain PWA smoke stays self-contained.
 */
const realApi = process.env['PW_REAL_API'];
const resetToken = process.env['E2E_RESET_TOKEN'] ?? 'local-e2e-reset-token';
// The e2e stack's seeded member; the workflow passes the seed's password in.
const member = {
  email: process.env['E2E_MEMBER_EMAIL'] ?? 'g1member1@projectk.com',
  password: process.env['E2E_MEMBER_PASSWORD'] ?? '',
};

test.skip(!realApi || !member.password, 'needs the e2e stack (PW_REAL_API, E2E_MEMBER_PASSWORD)');

test.beforeAll(async ({ request }) => {
  const reset = await request.post(`${realApi}/test/e2e/reset`, {
    headers: { 'X-E2E-Reset-Token': resetToken },
  });
  expect(reset.ok(), `E2E reset failed with ${reset.status()}`).toBe(true);
});

test('a member signs in, survives a reload and signs out against the real API', async ({ page }, info) => {
  await page.goto('./');
  await expect(page).toHaveURL(/\/m\/login$/);

  await page.locator('[data-testid=email] input').fill(member.email);
  await page.locator('[data-testid=password] input').fill('WrongPassword123!');
  await page.getByRole('button', { name: 'Увійти' }).click();
  await expect(page.getByRole('alert')).toHaveText('Невірний email або пароль.');

  await page.locator('[data-testid=password] input').fill(member.password);
  await page.getByRole('button', { name: 'Увійти' }).click();
  await expect(page.getByText(`Ти увійшов як ${member.email}`)).toBeVisible();
  await page.screenshot({ path: `test-results/pwa-screens/${info.project.name}-real-home.png`, scale: 'css' });

  // The refresh cookie the API set must bring the session back after a reload.
  const refreshed = page.waitForResponse((r) => r.url().endsWith('/auth/refresh'));
  await page.reload();
  expect((await refreshed).status()).toBe(200);
  await expect(page.getByText(`Ти увійшов як ${member.email}`)).toBeVisible();

  await page.getByText('Ще', { exact: true }).click();
  const loggedOut = page.waitForResponse((r) => r.url().endsWith('/auth/logout'));
  await page.getByText('Вийти', { exact: true }).click();
  expect((await loggedOut).ok()).toBe(true);
  await expect(page).toHaveURL(/\/m\/login$/);

  // The cookie is gone too: a fresh visit stays on the sign-in screen.
  await page.goto('./tabs/home');
  await expect(page).toHaveURL(/\/m\/login$/);
});
