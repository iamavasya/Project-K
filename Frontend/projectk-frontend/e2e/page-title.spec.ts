import { expect, test } from '@playwright/test';

import { getFirstSeededGroupMemberKey, getSeededGroupKey, getSeededKurinKey } from './support/api-client';
import { describeRole } from './support/role-test';

// The middle dot is the separator defined in page-title.format.ts.
const APP_NAME = 'ProjectK';
const PANEL_TITLE = new RegExp(`^Адміністрація · ${APP_NAME}$`);
// Admin subpages carry their own name only — the "Адміністрація" prefix made titles
// long enough to be truncated in the tab, hiding the part that distinguishes them.
const USERS_TITLE = new RegExp(`^Користувачі · ${APP_NAME}$`);
const KURIN_TITLE = new RegExp(`^к\\. ч\\. \\d+ · ${APP_NAME}$`);
const GROUP_TITLE = new RegExp(`^г\\. .+ · ${APP_NAME}$`);
// Last name then first name, so neither the fallback route title nor a blank entity
// name is what ends up in the tab.
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

  // Manager rather than admin: the seeded admin is not scoped to a kurin, so /kurin has
  // no key to resolve a number from.
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
