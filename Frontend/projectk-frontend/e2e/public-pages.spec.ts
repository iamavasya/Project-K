import { expect, test } from '@playwright/test';

test.describe('public pages', () => {
  test('welcome, join and login pages render without authentication', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('body')).toContainText(/ProjectK|Project K|Пласт|Увійти|Заяв/);

    await page.goto('/join');
    await expect(page).toHaveURL(/\/join/);
    await expect(page.locator('body')).toContainText(/заяв|Заяв|Email|Край|Станиц/);

    await page.goto('/login');
    await expect(page.locator('#email')).toBeVisible();
    await expect(page.locator('#password')).toBeVisible();
  });
});
