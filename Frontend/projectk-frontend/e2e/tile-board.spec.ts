import { expect, Page, test } from '@playwright/test';

import { getFirstSeededGroupMemberKey } from './support/api-client';
import { describeRole } from './support/role-test';

const SKILLS_HEADING = 'Здобуті вмілості';
const PROBES_HEADING = 'Проба';

/** Returns the index of the first tile whose text contains `needle`, or -1. */
async function tileIndexContaining(page: Page, needle: string): Promise<number> {
  const texts = await page.locator('.tile-board > .tile-slot').allInnerTexts();
  return texts.findIndex(text => text.includes(needle));
}

test.describe('Tile board personal layout', () => {
  describeRole('manager', 'Manager', ({ user }) => {
    test('reorders member-card tiles, persists across reload, and resets', async ({ page, request }) => {
      const memberKey = await getFirstSeededGroupMemberKey(request, user);

      await page.goto(`/member/${memberKey}`);
      await expect(page.locator('.tile-board')).toBeVisible();

      // Default order: skills tile comes before probes tile.
      const skillsBefore = await tileIndexContaining(page, SKILLS_HEADING);
      const probesBefore = await tileIndexContaining(page, PROBES_HEADING);
      expect(skillsBefore).toBeGreaterThanOrEqual(0);
      expect(probesBefore).toBeGreaterThan(skillsBefore);

      // Enter edit mode and nudge the probes tile up above skills.
      await page.getByRole('button', { name: 'Налаштувати вигляд' }).click();
      const probesTile = page.locator('.tile-board > .tile-slot', { hasText: PROBES_HEADING });
      await probesTile.getByRole('button', { name: 'Пересунути вгору' }).click();
      await page.getByRole('button', { name: 'Готово' }).click();

      // Probes now sits before skills.
      await expect
        .poll(() => tileIndexContaining(page, PROBES_HEADING))
        .toBeLessThan(await tileIndexContaining(page, SKILLS_HEADING));

      // Order survives a reload (persisted on the server, not just in memory).
      await page.reload();
      await expect(page.locator('.tile-board')).toBeVisible();
      await expect
        .poll(() => tileIndexContaining(page, PROBES_HEADING))
        .toBeLessThan(await tileIndexContaining(page, SKILLS_HEADING));

      // Reset restores the default order.
      await page.getByRole('button', { name: 'Налаштувати вигляд' }).click();
      await page.getByRole('button', { name: 'Скинути до стандартного' }).click();
      await page.getByRole('button', { name: 'Готово' }).click();

      await expect
        .poll(() => tileIndexContaining(page, SKILLS_HEADING))
        .toBeLessThan(await tileIndexContaining(page, PROBES_HEADING));
    });
  });
});
