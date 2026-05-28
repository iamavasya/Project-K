import { expect, test } from '@playwright/test';
import { getSeededKurinKey } from './support/api-client';
import { describeRole } from './support/role-test';

describeRole('admin', 'Admin role access', () => {
  test('admin can open global admin pages', async ({ page }) => {
    await page.goto('/panel');
    await expect(page).toHaveURL(/\/panel/);
    await expect(page.locator('body')).not.toContainText('Forbidden');

    await page.goto('/waitlist');
    await expect(page).toHaveURL(/\/waitlist/);
    await expect(page.locator('body')).not.toContainText('Forbidden');

    await page.goto('/users');
    await expect(page).toHaveURL(/\/users/);
    await expect(page.locator('body')).not.toContainText('Forbidden');

    await page.goto('/announcements');
    await expect(page).toHaveURL(/\/announcements/);
    await expect(page.locator('body')).not.toContainText('Forbidden');
  });

  test('admin is redirected from kurin to panel if they have no kurinKey', async ({ page }) => {
    await page.goto('/kurin');
    await expect(page).toHaveURL(/\/panel/);
  });
});

describeRole('manager', 'Manager role access', ({ user }) => {
  test('manager can access kurin and planning create but not admin pages', async ({ page, request }) => {
    await page.goto('/kurin');
    await expect(page).toHaveURL(/\/kurin/);
    await expect(page.locator('body')).not.toContainText('Forbidden');

    await page.goto('/panel');
    await expect(page).toHaveURL(/\/kurin/);

    await page.goto('/users');
    await expect(page).toHaveURL(/\/kurin/);

    const kurinKey = await getSeededKurinKey(request, user);
    await page.goto(`/planning/create/${kurinKey}`);
    await expect(page).toHaveURL(new RegExp(`/planning/create/${kurinKey}`));
    await expect(page.locator('body')).not.toContainText('Forbidden');
  });
});

describeRole('mentor', 'Mentor role access', ({ user }) => {
  test('mentor can access kurin and planning list but not create planning or admin pages', async ({ page, request }) => {
    await page.goto('/kurin');
    await expect(page).toHaveURL(/\/kurin/);
    await expect(page.locator('body')).not.toContainText('Forbidden');

    await expect(page.getByRole('button', { name: 'Р РµРґР°РіСѓРІР°С‚Рё' })).toBeHidden();
    await expect(page.getByRole('button', { name: 'РЎС‚РІРѕСЂРёС‚Рё' })).toBeHidden();
    await expect(page.getByRole('button', { name: 'Р”РѕРґР°С‚Рё СѓС‡Р°СЃРЅРёРєР° РєСѓСЂРµРЅСЏ' })).toBeHidden();

    for (const adminPage of ['/waitlist', '/users', '/announcements', '/panel']) {
      await page.goto(adminPage);
      await expect(page).toHaveURL(/\/kurin/);
    }

    const kurinKey = await getSeededKurinKey(request, user);
    await page.goto(`/planning/${kurinKey}`);
    await expect(page).toHaveURL(new RegExp(`/planning/${kurinKey}`));
    await expect(page.locator('body')).not.toContainText('Forbidden');

    await page.goto(`/planning/create/${kurinKey}`);
    await expect(page).toHaveURL(/\/forbidden/);
  });
});

describeRole('member', 'Member role access', ({ user }) => {
  test('member can access kurin but not planning list, planning create, or admin pages', async ({ page, request }) => {
    await page.goto('/kurin');
    await expect(page).toHaveURL(/\/kurin/);
    await expect(page.locator('body')).not.toContainText('Forbidden');

    await expect(page.getByRole('button', { name: 'Р РµРґР°РіСѓРІР°С‚Рё' })).toBeHidden();
    await expect(page.getByRole('button', { name: 'РЎС‚РІРѕСЂРёС‚Рё' })).toBeHidden();
    await expect(page.getByRole('button', { name: 'Р”РѕРґР°С‚Рё СѓС‡Р°СЃРЅРёРєР° РєСѓСЂРµРЅСЏ' })).toBeHidden();

    for (const adminPage of ['/panel', '/waitlist', '/users', '/announcements']) {
      await page.goto(adminPage);
      await expect(page).toHaveURL(/\/kurin/);
    }

    const kurinKey = await getSeededKurinKey(request, user);
    await page.goto(`/planning/${kurinKey}`);
    await expect(page).toHaveURL(/\/kurin/);

    await page.goto(`/planning/create/${kurinKey}`);
    await expect(page).toHaveURL(/\/forbidden/);
  });
});
