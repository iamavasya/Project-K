import { expect, test } from '@playwright/test';

import { getSeededGroupKey, getSeededKurinKey } from './support/api-client';
import { describeRole } from './support/role-test';
import { openGroupActionMenu, visibleMenuItems } from './support/ui';

describeRole('manager', 'Manager conditional organization UI', ({ user }) => {
  test('manager sees kurin-level management controls', async ({ page }) => {
    await page.goto('/kurin');

    await expect(page.locator('.edit-profile-button')).toBeVisible();
    await expect(page.locator('.group-panel .table-caption').getByRole('button')).toBeVisible();
    await expect(page.locator('app-member-list + div').getByRole('button')).toBeVisible();
    await expect(page.locator('app-kv-panel .kv-caption__actions button:has(.pi-plus)')).toBeVisible();
    await expect(page.locator('app-leadership-panel .leadership-caption__actions button:has(.pi-cog)').first()).toBeVisible();
  });

  test('manager group action menu exposes profile members mentors and silhouette actions', async ({ page, request }) => {
    const groupKey = await getSeededGroupKey(request, user, 'Gurtok 1');

    await page.goto(`/group/${groupKey}`);
    await expect(page.locator('body')).toContainText('Gurtok 1');
    await expect(page.locator('.group-actions').getByRole('button')).toBeVisible();
    await openGroupActionMenu(page);

    expect(await visibleMenuItems(page).count()).toBeGreaterThanOrEqual(4);
  });

  test('manager planning list exposes create and destructive row controls', async ({ page, request }) => {
    const kurinKey = await getSeededKurinKey(request, user);

    await page.goto(`/planning/${kurinKey}`);
    await expect(page.locator('button:has(.pi-plus)')).toBeVisible();

    const rows = page.locator('tr.planning-row');
    if (await rows.count()) {
      await expect(page.locator('button:has(.pi-trash)').first()).toBeVisible();
    }
  });
});

describeRole('mentor', 'Mentor conditional organization UI', ({ user }) => {
  test('mentor sees read-only kurin controls and no kurin-wide management buttons', async ({ page }) => {
    await page.goto('/kurin');

    await expect(page.locator('.edit-profile-button')).toBeHidden();
    await expect(page.locator('.group-panel .table-caption').getByRole('button')).toBeHidden();
    await expect(page.locator('app-member-list + div').getByRole('button')).toBeHidden();
    await expect(page.locator('app-kv-panel .kv-caption__actions button:has(.pi-plus)')).toBeHidden();
    await expect(page.locator('app-leadership-panel .leadership-caption__actions button:has(.pi-cog)').first()).toBeHidden();
  });

  test('mentor group action menu only allows adding members in an assigned group', async ({ page, request }) => {
    const groupKey = await getSeededGroupKey(request, user, 'Gurtok 1');

    await page.goto(`/group/${groupKey}`);
    await expect(page.locator('body')).toContainText('Gurtok 1');
    await openGroupActionMenu(page);
    await expect(visibleMenuItems(page)).toHaveCount(1);

    await visibleMenuItems(page).first().click();
    await expect(page).toHaveURL(new RegExp(`/group/${groupKey}/member/upsert`));
  });

  test('mentor planning list is inspect-only', async ({ page, request }) => {
    const kurinKey = await getSeededKurinKey(request, user);

    await page.goto(`/planning/${kurinKey}`);
    await expect(page.locator('button:has(.pi-plus)')).toBeHidden();
    await expect(page.locator('button:has(.pi-trash)')).toBeHidden();
  });
});

describeRole('member', 'Member conditional organization UI', ({ user }) => {
  test('member has no kurin or group management controls', async ({ page, request }) => {
    const groupKey = await getSeededGroupKey(request, user, 'Gurtok 1');

    await page.goto('/kurin');
    await expect(page.locator('.edit-profile-button')).toBeHidden();
    await expect(page.locator('.group-panel .table-caption').getByRole('button')).toBeHidden();
    await expect(page.locator('app-member-list + div').getByRole('button')).toBeHidden();
    await expect(page.locator('app-kv-panel .kv-caption__actions button:has(.pi-plus)')).toBeHidden();
    await expect(page.locator('app-leadership-panel .leadership-caption__actions button:has(.pi-cog)').first()).toBeHidden();

    await page.goto(`/group/${groupKey}`);
    await expect(page.locator('body')).toContainText('Gurtok 1');
    await expect(page.locator('.group-actions').getByRole('button')).toBeHidden();
  });
});
