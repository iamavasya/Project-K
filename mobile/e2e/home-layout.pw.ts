import { expect, test } from '@playwright/test';
import { member, ok, refuse, shot, signIn, type ExtraApi } from './support/mock-api';

/**
 * Головна follows the board the person arranged on the web's Головна (web tile-board, board
 * `dashboard`): the tiles they hid stay away and the rest come in their order. Read only; with no
 * layout, or none to read, every tile shows.
 */

const layouts = (dashboard: { tileKeys: string[]; hiddenTileKeys: string[] }): ExtraApi => ({ method, path }) =>
  method === 'GET' && path === 'user/me/layouts'
    ? ok([
        { boardKey: 'member-card', tileKeys: ['profile'], hiddenTileKeys: [], schemaVersion: 1, updatedAtUtc: '2026-10-01T10:00:00Z' },
        { boardKey: 'dashboard', ...dashboard, schemaVersion: 1, updatedAtUtc: '2026-10-01T10:00:00Z' },
      ])
    : null;

const cardTitles = (page: import('@playwright/test').Page) =>
  page.locator('app-home ion-card ion-card-title, app-home ion-card ion-card-subtitle').allInnerTexts();

test('leaves out the tiles hidden on the web and keeps the web’s order', async ({ page }, info) => {
  await signIn(page, member, layouts({
    // «Мій профіль» and «Мої курені» are the web's own; the phone has them in «Меню».
    tileKeys: ['my-profile', 'my-dues', 'upcoming-events', 'my-probe', 'my-skills', 'my-tasks', 'my-score', 'my-kurins'],
    hiddenTileKeys: ['my-tasks', 'my-score'],
  }));
  await expect(page.getByText('Мої задачі')).toHaveCount(0);
  await expect(page.getByText(/балів/)).toHaveCount(0);
  await expect(page.getByText('Вмілості')).toBeVisible();
  const titles = (await cardTitles(page)).join(' | ');
  // Вкладка, then Найближче, then the probe and the skills.
  expect(titles.indexOf('Вкладка')).toBeLessThan(titles.indexOf('Найближче'));
  expect(titles.indexOf('Найближче')).toBeLessThan(titles.indexOf('Проба'));
  expect(titles.indexOf('Проба')).toBeLessThan(titles.indexOf('Вмілості'));
  // Kept on the device as the web keeps it, so the next start shows no hidden tile even for a moment.
  expect(await page.evaluate(() => JSON.parse(localStorage.getItem('tile-layout:dashboard') ?? 'null')?.hiddenTileKeys)).toEqual([
    'my-tasks',
    'my-score',
  ]);
  // The sign-in page is still sliding away on a slow WebKit.
  await expect(page.locator('app-login')).toHaveCount(0);
  await shot(page, info.project.name, 'home-layout');

  await page.reload();
  await expect(page.getByText('Сходини гуртка')).toBeVisible();
  await expect(page.getByText('Мої задачі')).toHaveCount(0);
});

test('shows every tile when the layout cannot be read', async ({ page }) => {
  await signIn(page, member, ({ method, path }) => (method === 'GET' && path === 'user/me/layouts' ? refuse(500) : null));
  await expect(page.getByText('Мої задачі')).toBeVisible();
  await expect(page.getByText(/балів/)).toBeVisible();
  const titles = (await cardTitles(page)).join(' | ');
  expect(titles.indexOf('Найближче')).toBeLessThan(titles.indexOf('Мої задачі'));
});
