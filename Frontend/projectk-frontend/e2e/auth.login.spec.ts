import { expect, test } from '@playwright/test';
import { e2eUsers } from './support/test-users';
import { expectAuthenticatedAs, loginThroughUi } from './support/login';

test.describe('login', () => {
  test('seeded manager can sign in without MFA in dev/e2e env', async ({ page }) => {
    await loginThroughUi(page, e2eUsers.manager);
    await expectAuthenticatedAs(page, e2eUsers.manager);
    await expect(page).not.toHaveURL(/\/login$/);
  });

  test('invalid password keeps user unauthenticated', async ({ page }) => {
    await page.goto('/login');
    await page.locator('#email').fill(e2eUsers.manager.email);
    await page.locator('#password').fill('WrongPassword123!');
    await page.locator('button[type="submit"]').click();

    await expect(page).toHaveURL(/\/login/);
    await expect.poll(
      async () => page.evaluate(() => localStorage.getItem('authState'))
    ).toBeNull();
  });
});
