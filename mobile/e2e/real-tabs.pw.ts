import { expect, test, type Page } from '@playwright/test';
import { shot } from './support/mock-api';

/**
 * Головна, Календар and Курінь against the real API (the e2e stack, as real-login.pw.ts): every
 * section answers, none fails, nothing runs past its edges with the seed's real names. As a member
 * and as провід (the stack lets провід in without the second factor).
 */
const realApi = process.env['PW_REAL_API'];
const resetToken = process.env['E2E_RESET_TOKEN'] ?? 'local-e2e-reset-token';
// The e2e seed's shared test password (Frontend/projectk-frontend/e2e/support/test-users.ts).
const password = process.env['E2E_MEMBER_PASSWORD'] ?? '';
const accounts = [
  { who: 'member', email: process.env['E2E_MEMBER_EMAIL'] ?? 'g1member1@projectk.com' },
  { who: 'leader', email: process.env['E2E_MANAGER_EMAIL'] ?? 'manager1@projectk.com' },
];

test.skip(!realApi || !password, 'needs the e2e stack (PW_REAL_API, E2E_MEMBER_PASSWORD)');

test.beforeAll(async ({ request }) => {
  const reset = await request.post(`${realApi}/test/e2e/reset`, {
    headers: { 'X-E2E-Reset-Token': resetToken },
  });
  expect(reset.ok(), `E2E reset failed with ${reset.status()}`).toBe(true);
});

/** The screen under `scope` has loaded: no skeletons left and no section saying it failed. */
async function settled(page: Page, scope: string): Promise<void> {
  await expect(page.locator(`${scope} ion-skeleton-text`)).toHaveCount(0, { timeout: 15_000 });
  await expect(page.locator(scope).getByText('Не вдалося завантажити', { exact: false })).toHaveCount(0);
}

for (const account of accounts) {
  test(`a ${account.who} goes through Головна, Календар and Курінь on the real API`, async ({ page }, info) => {
    const project = info.project.name;
    // Any API call that fails, sign-in aside (a first visit has no refresh cookie yet).
    const failed: string[] = [];
    page.on('response', (r) => {
      if (r.url().startsWith(realApi!) && !r.url().includes('/auth/') && r.status() >= 400) {
        failed.push(`${r.status()} ${r.request().method()} ${r.url()}`);
      }
    });

    await page.goto('./');
    await expect(page).toHaveURL(/\/m\/login$/);
    await page.locator('[data-testid=email] input').fill(account.email);
    await page.locator('[data-testid=password] input').fill(password);
    await page.getByRole('button', { name: 'Увійти' }).click();
    await expect(page).toHaveURL(/\/m\/tabs\/home$/);
    await settled(page, 'app-home');
    await shot(page, project, `real-${account.who}-home`);

    await page.locator('#tab-button-calendar').click();
    await expect(page.getByTestId('month')).toBeVisible();
    await settled(page, 'app-calendar');
    await shot(page, project, `real-${account.who}-calendar`);

    await page.locator('#tab-button-kurin').click();
    await expect(page.getByTestId('kurin-head')).toBeVisible();
    await settled(page, 'app-kurin');
    await shot(page, project, `real-${account.who}-kurin`);

    // The first гурток and the first member open with everything they read.
    const group = page.getByTestId('groups').locator('ion-item').filter({ has: page.locator('app-group-silhouette') }).first();
    if (await group.count()) {
      await group.click();
      await expect(page.getByTestId('group-head')).toBeVisible();
      await settled(page, 'app-group');
      await shot(page, project, `real-${account.who}-group`);
      await page.locator('app-group ion-back-button').click();
      await expect(page.getByTestId('kurin-head')).toBeVisible();
    }
    const member = page.getByTestId('member').first();
    if (await member.count()) {
      await member.click();
      await expect(page.getByTestId('member-head')).toBeVisible();
      await settled(page, 'app-member');
      await shot(page, project, `real-${account.who}-member`);
    }

    expect(failed).toEqual([]);
  });
}
