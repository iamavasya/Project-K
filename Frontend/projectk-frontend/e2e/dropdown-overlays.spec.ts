import { expect, Locator, Page, test } from '@playwright/test';

import { getFirstSeededGroupMemberKey } from './support/api-client';
import { describeRole } from './support/role-test';

const PANEL = '.p-select-overlay, .p-multiselect-overlay';

async function expectPanelReachable(page: Page, trigger: Locator): Promise<void> {
  await trigger.scrollIntoViewIfNeeded();
  await trigger.click();
  const panel = page.locator(PANEL).last();
  await expect(panel).toBeVisible();

  const box = (await panel.boundingBox())!;
  const viewport = page.viewportSize()!;
  expect(box.y).toBeGreaterThanOrEqual(0);
  expect(box.y).toBeLessThan(viewport.height);
  expect(box.width).toBeGreaterThan(120);

  const onTop = await page.evaluate(({ x, y, selector }) =>
    !!document.elementFromPoint(x, y)?.closest(selector),
    { x: box.x + box.width / 2, y: box.y + Math.min(24, box.height / 2), selector: PANEL });
  expect(onTop).toBe(true);

  await panel.locator('[role=option]').first().click();
}

async function expectPageUnlocked(page: Page): Promise<void> {
  await expect.poll(() => page.evaluate(() => ({
    locked: getComputedStyle(document.body).overflow === 'hidden' || document.body.classList.contains('p-overflow-hidden'),
    masks: document.querySelectorAll('.p-overlay-mask').length
  }))).toEqual({ locked: false, masks: 0 });
}

describeRole('manager', 'Dropdowns on a scrolled page and inside dialogs', ({ user }) => {
  test('the registry column picker leaves the page scrollable once it closes', async ({ page }) => {
    await page.goto('/kurin/registry');
    await expectPanelReachable(page, page.locator('.registry__columns'));
    await page.keyboard.press('Escape');
    await expectPageUnlocked(page);
  });

  test('the move-to-group picker opens above its dialog on a scrolled member card', async ({ page, request }) => {
    const memberKey = await getFirstSeededGroupMemberKey(request, user, 'Gurtok 1');
    await page.goto(`/member/${memberKey}`);

    await page.locator('app-member-memberships-tile button', { hasText: /^\s*Гурток\s*$/ }).click();
    const dialog = page.locator('.p-dialog', { hasText: 'Перевести в гурток' });
    await expect(dialog).toBeVisible();
    await expectPanelReachable(page, dialog.locator('.p-select'));

    await dialog.getByRole('button', { name: 'Скасувати' }).click();
    await expect(dialog).toBeHidden();
    await expectPageUnlocked(page);
  });
});
