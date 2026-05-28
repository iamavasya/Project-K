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

  test('admin can create, edit, and delete an announcement', async ({ page }) => {
    await page.goto('/announcements');
    await expect(page).toHaveURL(/\/announcements/);
    const suffix = Date.now();
    const title = `E2E Test Announcement ${suffix}`;
    const editedTitle = `E2E Test Announcement Edited ${suffix}`;
    
    // 1. Create new draft
    await page.getByRole('button', { name: 'New draft' }).click();
    
    // Fill the dialog
    await page.getByLabel('Title').fill(title);
    await page.getByLabel('Body').fill('This is a test announcement created by E2E test.');
    await page.getByRole('button', { name: 'Save' }).click();

    // Wait for it to appear in the table
    const row = page.locator('tr', { hasText: title }).first();
    await expect(row).toBeVisible();

    // Close the dialog using the Close button after the async save refresh has finished.
    await page.getByRole('dialog').getByRole('button', { name: 'Close' }).click();
    await expect(page.getByRole('dialog')).toBeHidden();

    // 2. Edit draft
    await row.locator('.pi-pencil').click();
    
    // Wait for dialog to open
    const dialog = page.getByRole('dialog');
    await expect(dialog).toBeVisible();
    
    await dialog.getByLabel('Title').fill(editedTitle);
    await dialog.getByRole('button', { name: 'Save' }).click();

    const editedRow = page.locator('tr', { hasText: editedTitle }).first();
    await expect(editedRow).toBeVisible();
    
    // Close the dialog
    await dialog.getByRole('button', { name: 'Close' }).click();
    await expect(dialog).toBeHidden();

    // 3. Delete draft
    await editedRow.locator('.pi-trash').click();
    // Confirm deletion
    const confirmButton = page.getByRole('alertdialog', { name: 'Delete' }).getByRole('button', { name: 'Yes' });
    await expect(confirmButton).toBeVisible();
    await confirmButton.click();
    
    // Deleted drafts stay in the table as a soft-deleted audit row.
    await expect(editedRow).toContainText('Deleted');
  });
});
