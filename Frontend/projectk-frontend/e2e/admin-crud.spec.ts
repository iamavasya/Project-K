import { expect, test } from '@playwright/test';
import { describeRole } from './support/role-test';

describeRole('admin', 'Admin CRUD operations', () => {
  test('admin can navigate and view users', async ({ page }) => {
    await page.goto('/users');
    await expect(page).toHaveURL(/\/users/);
    
    // Verify table is visible
    await expect(page.locator('h2', { hasText: 'Користувачі' })).toBeVisible();
    await expect(page.locator('.p-datatable')).toBeVisible();

    // Verify search works visually
    const searchInput = page.getByPlaceholder('Пошук за імʼям або email');
    await expect(searchInput).toBeVisible();

    // Expand the first kurin group to see users if it is collapsed
    const expandChevron = page.locator('.pi-chevron-right').first();
    if (await expandChevron.isVisible()) {
      await expandChevron.click();
    }

    // Verify at least one user row is visible
    const firstRow = page.locator('.p-datatable-tbody > tr, tr.p-selectable-row').first();
    await expect(firstRow).toBeVisible();
  });
});
