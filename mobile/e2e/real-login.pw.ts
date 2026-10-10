import { createHmac } from 'node:crypto';
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

/** RFC 6238 code for an authenticator key as the API shows it (base32, grouped, any case). */
function totp(sharedKey: string, at = Date.now()): string {
  const alphabet = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ234567';
  const bits = [...sharedKey.replace(/[\s=]/g, '').toUpperCase()]
    .map((c) => alphabet.indexOf(c).toString(2).padStart(5, '0'))
    .join('');
  const key = Buffer.from(bits.match(/.{8}/g)!.map((b) => parseInt(b, 2)));
  const counter = Buffer.alloc(8);
  counter.writeBigUInt64BE(BigInt(Math.floor(at / 30_000)));
  const hmac = createHmac('sha1', key).update(counter).digest();
  const offset = hmac[hmac.length - 1] & 0xf;
  return String((hmac.readUInt32BE(offset) & 0x7fffffff) % 1_000_000).padStart(6, '0');
}

test('a member uses the app against the real API: sign-in, dashboard, profile, two-factor, sign-out', async ({ page }, info) => {
  const failed: string[] = [];
  page.on('response', (r) => {
    if (/\/api\/(me|member)\//.test(r.url()) && r.status() >= 400) failed.push(`${r.status()} ${r.url()}`);
  });

  await page.goto('./');
  await expect(page).toHaveURL(/\/m\/login$/);

  await page.locator('[data-testid=email] input').fill(member.email);
  await page.locator('[data-testid=password] input').fill('WrongPassword123!');
  await page.getByRole('button', { name: 'Увійти' }).click();
  await expect(page.getByRole('alert')).toHaveText('Невірний email або пароль.');

  await page.locator('[data-testid=password] input').fill(member.password);
  await page.getByRole('button', { name: 'Увійти' }).click();
  await expect(page).toHaveURL(/\/m\/tabs\/home$/);
  // Every section has answered once the skeletons are gone, and none of them failed.
  await expect(page.locator('app-home ion-skeleton-text')).toHaveCount(0);
  await expect(page.getByText('Не вдалося завантажити', { exact: false })).toHaveCount(0);
  expect(failed).toEqual([]);
  await page.screenshot({ path: `test-results/pwa-screens/${info.project.name}-real-home.png`, scale: 'css' });

  // The refresh cookie the API set must bring the session back after a reload.
  const refreshed = page.waitForResponse((r) => r.url().endsWith('/auth/refresh'));
  await page.reload();
  expect((await refreshed).status()).toBe(200);
  await expect(page.locator('app-home ion-skeleton-text')).toHaveCount(0);
  await expect(page.getByText('Не вдалося завантажити', { exact: false })).toHaveCount(0);

  await page.getByText('Ще', { exact: true }).click();
  await expect(page.getByText('вимкнено', { exact: true })).toBeVisible();
  await page.getByText('Профіль', { exact: true }).click();
  await expect(page.locator('app-profile h1')).toBeVisible();
  await expect(page.getByText(member.email, { exact: true })).toBeVisible();
  await page.screenshot({ path: `test-results/pwa-screens/${info.project.name}-real-profile.png`, scale: 'css' });

  // Turning the second factor on, with a code computed from the key the API handed out.
  await page.goto('./mfa');
  const sharedKey = (await page.getByTestId('shared-key').textContent()) ?? '';
  expect(sharedKey.trim().length).toBeGreaterThan(10);
  await page.locator('[data-testid=mfa-code] input').filter({ visible: true }).fill(totp(sharedKey));
  await page.getByRole('button', { name: 'Увімкнути' }).click();
  await expect(page.getByTestId('recovery-codes')).not.toBeEmpty();
  await page.getByRole('button', { name: 'Я зберіг коди' }).click();
  await expect(page).toHaveURL(/\/m\/tabs\/home$/);
  await page.getByText('Ще', { exact: true }).click();
  await expect(page.getByText('увімкнено', { exact: true })).toBeVisible();

  // Enabling ended the old session; signing out works only with the token it handed back.
  const loggedOut = page.waitForResponse((r) => r.url().endsWith('/auth/logout'));
  await page.getByText('Вийти', { exact: true }).click();
  expect((await loggedOut).ok()).toBe(true);
  await expect(page).toHaveURL(/\/m\/login$/);

  // The cookie is gone too: a fresh visit stays on the sign-in screen.
  await page.goto('./tabs/home');
  await expect(page).toHaveURL(/\/m\/login$/);
  expect(failed).toEqual([]);
});
