import { expect, test, type Page } from '@playwright/test';
import { fakePush, standalone } from './support/fake-push';
import { type ExtraApi, member, ok, shot, signIn } from './support/mock-api';

/**
 * «Сповіщення на телефон» in «Меню», against the stand-in API, with the browser's push machinery
 * faked in the page (support/fake-push.ts): Angular's SwPush and the app's code run on it unchanged.
 */

// The e2e stack's throwaway VAPID public key (docker/env/e2e.env); any 65-byte key would do here.
const KEY = 'BDeLW-FFPXDIu7DhSQo_OnzzEmrLljJO2-0bszZ5cxFscIcrosM2WTzjOGuZZOCWwujNEObZm0iLGconJxSdIcg';

const pushApi: ExtraApi = ({ method, path }) => {
  if (method === 'GET' && path === 'notifications/push/public-key') return ok({ publicKey: KEY });
  if (path === 'notifications/push/subscriptions') return ok(true);
  return null;
};

async function openMenu(page: Page): Promise<void> {
  await page.locator('#tab-button-more').click();
  await expect(row(page)).toBeVisible();
  // Into the middle: «if needed» counts a row under the tab bar as visible.
  await row(page).evaluate((el) => el.scrollIntoView({ block: 'center' }));
}

const row = (page: Page) => page.locator('app-more').getByTestId('push-row');

test('turns pushes to this phone on and off from «Меню», and keeps the server up to date', async ({ page }, info) => {
  const project = info.project.name;
  await fakePush(page, { permission: 'default', answer: 'granted' });
  await standalone(page);
  const log = await signIn(page, member, pushApi);
  const subscribes = () => log.writes.filter((w) => w.method === 'POST' && w.path === 'notifications/push/subscriptions');

  await openMenu(page);
  await expect(row(page)).toHaveAttribute('data-state', 'off');
  await shot(page, project, 'push-off');

  await row(page).getByTestId('push-toggle').click();
  await expect(row(page)).toHaveAttribute('data-state', 'on');
  await expect(row(page).getByTestId('push-toggle')).toHaveJSProperty('checked', true);
  expect(subscribes()).toHaveLength(1);
  const sent = subscribes()[0].body;
  expect(sent.endpoint).toMatch(/^https:\/\/fcm\.googleapis\.com\/fcm\/send\/e2e-/);
  expect(sent.keys.p256dh).toHaveLength(87);
  expect(sent.keys.auth).toHaveLength(22);
  await shot(page, project, 'push-on');

  // The next start sends the subscription again: endpoints rotate, the server keeps the last one.
  await page.reload();
  await expect(row(page)).toHaveAttribute('data-state', 'on');
  await expect.poll(() => subscribes().length).toBe(2);
  expect(subscribes()[1].body.endpoint).toBe(sent.endpoint);

  await row(page).getByTestId('push-toggle').click();
  await expect(row(page)).toHaveAttribute('data-state', 'off');
  expect(log.writes.filter((w) => w.method === 'DELETE' && w.path === 'notifications/push/subscriptions')).toHaveLength(1);
  expect(await page.evaluate(() => localStorage.getItem('e2e-push-endpoint'))).toBeNull();
});

test('says how to allow notifications once the phone forbids them', async ({ page }, info) => {
  await fakePush(page, { permission: 'default', answer: 'denied' });
  await standalone(page);
  const log = await signIn(page, member, pushApi);

  await openMenu(page);
  await row(page).getByTestId('push-toggle').click();
  await expect(row(page)).toHaveAttribute('data-state', 'blocked');
  await expect(row(page).getByTestId('push-hint')).toHaveText(/налаштуваннях телефону/);
  expect(log.writes.filter((w) => w.path.startsWith('notifications/push'))).toHaveLength(0);
  await shot(page, info.project.name, 'push-blocked');
});

test('asks an iPhone to add the app to the home screen first', async ({ page }, info) => {
  test.skip(info.project.name !== 'iphone', 'only iOS limits push to the home-screen app');
  await fakePush(page, { permission: 'default', noPushApi: true });
  await signIn(page, member, pushApi);

  await openMenu(page);
  await expect(row(page)).toHaveAttribute('data-state', 'install');
  await expect(row(page).getByTestId('push-hint')).toHaveText(/На початковий екран/);
  await expect(row(page).getByTestId('push-toggle')).toHaveCount(0);
  await shot(page, info.project.name, 'push-install');
});

test('says so in a browser without push', async ({ page }, info) => {
  test.skip(info.project.name !== 'android', 'an iPhone in a Safari tab is asked to install instead');
  await fakePush(page, { permission: 'default', noPushApi: true });
  await signIn(page, member, pushApi);

  await openMenu(page);
  await expect(row(page)).toHaveAttribute('data-state', 'unsupported');
  await shot(page, info.project.name, 'push-unsupported');
});

test('hides the row where the server sends no pushes', async ({ page }) => {
  await fakePush(page, { permission: 'default' });
  await standalone(page);
  await signIn(page, member, ({ method, path }) =>
    method === 'GET' && path === 'notifications/push/public-key' ? ok({ publicKey: null }) : null,
  );

  await page.locator('#tab-button-more').click();
  await expect(page.locator('app-more').getByText('Вигляд')).toBeVisible();
  await expect(row(page)).toHaveCount(0);
});

test('signing out forgets this device on the server and in the browser', async ({ page }) => {
  await fakePush(page, { permission: 'default', answer: 'granted' });
  await standalone(page);
  const log = await signIn(page, member, pushApi);
  await openMenu(page);
  await row(page).getByTestId('push-toggle').click();
  await expect(row(page)).toHaveAttribute('data-state', 'on');

  const loggedOut = page.waitForResponse((r) => r.url().endsWith('/auth/logout'));
  await page.locator('app-more').getByText('Вийти', { exact: true }).click();
  await page.locator('ion-action-sheet').getByRole('button', { name: 'Вийти' }).click();
  expect((await loggedOut).ok()).toBe(true);
  await expect(page).toHaveURL(/\/m\/login$/);

  expect(log.writes.filter((w) => w.method === 'DELETE' && w.path === 'notifications/push/subscriptions')).toHaveLength(1);
  expect(await page.evaluate(() => localStorage.getItem('e2e-push-endpoint'))).toBeNull();
});
