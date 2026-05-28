import { expect, test } from '@playwright/test';

import { getSeededGroupKey, getSeededKurinKey } from './support/api-client';
import { describeRole } from './support/role-test';
import { chooseMenuItem, dialog, fillManagePanelFields, openRowMenu, submitManagePanel } from './support/ui';

describeRole('manager', 'Organization management surfaces', ({ user }) => {
  test('manager can create update and delete a group through manage-panel', async ({ page }) => {
    const groupName = `E2E Group ${Date.now()}`;
    const editedGroupName = `${groupName} Edited`;

    await page.goto('/kurin');
    await page.locator('.group-panel .table-caption').getByRole('button').click();
    await expect(dialog(page)).toBeVisible();
    await fillManagePanelFields(page, [groupName]);
    await submitManagePanel(page);

    const row = page.locator('tr', { hasText: groupName }).first();
    await expect(row).toBeVisible();

    await openRowMenu(row);
    await chooseMenuItem(page, 0);
    await fillManagePanelFields(page, [editedGroupName]);
    await submitManagePanel(page);

    const editedRow = page.locator('tr', { hasText: editedGroupName }).first();
    await expect(editedRow).toBeVisible();

    await openRowMenu(editedRow);
    await chooseMenuItem(page, 1);
    await submitManagePanel(page);
    await expect(editedRow).toBeHidden();
  });

  test('kv assignment and manager transfer dialogs expose disabled safe defaults', async ({ page }) => {
    await page.goto('/kurin');
    const kvPanel = page.locator('app-kv-panel');
    await expect(kvPanel).toBeVisible();

    await kvPanel.locator('.kv-caption__actions').getByRole('button').first().click();
    await expect(dialog(page)).toBeVisible();
    await expect(dialog(page).locator('.kv-dialog-actions').getByRole('button').last()).toBeDisabled();
    await dialog(page).locator('.kv-dialog-actions').getByRole('button').first().click();

    await kvPanel.locator('.kv-caption__actions').getByRole('button').nth(1).click();
    await expect(dialog(page)).toBeVisible();
    await expect(dialog(page).locator('.kv-dialog-actions').getByRole('button').last()).toBeDisabled();
    await dialog(page).locator('.kv-dialog-actions').getByRole('button').first().click();
  });

  test('leadership setup form loads for kurin and group contexts with required rows', async ({ page, request }) => {
    const kurinKey = await getSeededKurinKey(request, user);
    const groupKey = await getSeededGroupKey(request, user, 'Gurtok 1');

    await page.goto(`/leadership/create/kurin/${kurinKey}`);
    await expect(page.locator('app-leadership-component')).toBeVisible();
    await expect(page.locator('.p-datatable')).toBeVisible();
    await expect(page.locator('app-leadership-component').getByRole('button').last()).toBeDisabled();

    await page.goto(`/leadership/create/group/${groupKey}`);
    await expect(page.locator('app-leadership-component')).toBeVisible();
    await expect(page.locator('.p-datatable')).toBeVisible();
    await expect(page.locator('app-leadership-component').getByRole('button').last()).toBeDisabled();
  });
});
