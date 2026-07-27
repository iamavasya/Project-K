import { expect, test } from '@playwright/test';

import { getFirstSeededGroupMemberKey, getSeededGroupKey, getSeededKurinKey } from './support/api-client';
import { describeRole } from './support/role-test';

const APP_NAME = 'Лілейка';
const PANEL_TITLE = new RegExp(`^Адміністрація · ${APP_NAME}$`);
const USERS_TITLE = new RegExp(`^Користувачі · ${APP_NAME}$`);
const KURIN_TITLE = new RegExp(`^к\\. ч\\. \\d+ · ${APP_NAME}$`);
const GROUP_TITLE = new RegExp(`^г\\. .+ · ${APP_NAME}$`);
const MEMBER_TITLE = new RegExp(`^\\S+ \\S+ · ${APP_NAME}$`);

test.describe('Browser tab title', () => {
  describeRole('admin', 'Admin', ({ user }) => {
    test('admin pages are titled after the administration area', async ({ page }) => {
      await page.goto('/panel');
      await expect(page).toHaveTitle(PANEL_TITLE);

      await page.goto('/users');
      await expect(page).toHaveTitle(USERS_TITLE);
    });
  });

  describeRole('manager', 'Manager', ({ user }) => {
    test('kurin, group and member pages are titled after the entity', async ({ page, request }) => {
      const kurinKey = await getSeededKurinKey(request, user);
      const groupKey = await getSeededGroupKey(request, user, 'Gurtok 1');
      const memberKey = await getFirstSeededGroupMemberKey(request, user);

      await page.goto('/kurin');
      await expect(page).toHaveTitle(KURIN_TITLE);

      await page.goto(`/planning/${kurinKey}`);
      await expect(page).toHaveTitle(KURIN_TITLE);

      await page.goto(`/group/${groupKey}`);
      await expect(page).toHaveTitle(GROUP_TITLE);

      await page.goto(`/member/${memberKey}`);
      await expect(page).toHaveTitle(MEMBER_TITLE);
    });
  });

  test('public pages fall back to the system name alone', async ({ page }) => {
    await page.goto('/welcome');
    await expect(page).toHaveTitle(APP_NAME);
  });
});
