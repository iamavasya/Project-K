import { expect, test, type BrowserContext, type Page } from '@playwright/test';

const shots = 'test-results/pwa-screens';
// What scripts/serve-www.mjs puts in /env.js.
const api = 'https://api.example.test/api';
const member = { email: 'yunak@example.com', password: 'Yunak@12345' };
const leader = { email: 'lead@example.com', password: 'Lead@12345' };

/**
 * A stand-in for the API's auth endpoints, with the refresh cookie kept as a flag. Routed on the
 * context so requests passing through the service worker are caught too.
 */
async function mockApi(context: BrowserContext): Promise<void> {
  let session = false;
  await context.route(`${api}/**`, async (route) => {
    const request = route.request();
    const headers = {
      'access-control-allow-origin': request.headers()['origin'] ?? '*',
      'access-control-allow-credentials': 'true',
      'access-control-allow-headers': 'content-type, authorization',
      'access-control-allow-methods': 'GET, POST, OPTIONS',
    };
    if (request.method() === 'OPTIONS') return route.fulfill({ status: 204, headers });
    const json = (status: number, body: unknown) => route.fulfill({ status, headers, json: body });
    const path = new URL(request.url()).pathname.replace('/api/', '');
    const body = request.postDataJSON() ?? {};
    const signedIn = (email: string) => {
      session = true;
      return json(200, {
        userKey: 'u1', memberKey: 'm1', email, isAdmin: false, permissions: [], roles: [],
        kurinKey: 'k1', requiresMfa: false, tokens: { accessToken: 'access-1' },
      });
    };

    switch (path) {
      case 'auth/login': {
        const account = [member, leader].find((a) => a.email === body.email && a.password === body.password);
        if (!account) return json(401, { error: 'InvalidCredentials', message: 'Invalid credentials' });
        if (account === leader) return json(200, { requiresMfa: true, mfaToken: 'mfa-1', tokens: null });
        return signedIn(account.email);
      }
      case 'auth/mfa/login-verify':
        return body.code === '123456' && body.mfaToken === 'mfa-1'
          ? signedIn(body.email)
          : json(401, { error: 'InvalidMfaCode', message: 'Invalid code' });
      case 'auth/refresh':
        return session ? json(200, { accessToken: 'access-2' }) : json(401, { error: 'Unauthorized' });
      case 'auth/logout':
        session = false;
        return route.fulfill({ status: 200, headers, body: '' });
      default:
        return json(404, {});
    }
  });
}

async function fillCredentials(page: Page, account: { email: string; password: string }): Promise<void> {
  await page.locator('[data-testid=email] input').fill(account.email);
  await page.locator('[data-testid=password] input').fill(account.password);
  await page.getByRole('button', { name: 'Увійти' }).click();
}

async function signIn(page: Page, account = member): Promise<void> {
  await mockApi(page.context());
  await page.goto('./');
  await expect(page).toHaveURL(/\/m\/login$/);
  await fillCredentials(page, account);
  await expect(page.getByText(`Ти увійшов як ${account.email}`)).toBeVisible();
}

async function shot(page: Page, project: string, name: string): Promise<void> {
  await page.waitForTimeout(400); // let Ionic transitions settle
  await page.screenshot({ path: `${shots}/${project}-${name}.png`, scale: 'css' });
}

test('serves an installable manifest', async ({ request }) => {
  const manifest = await (await request.get('manifest.webmanifest')).json();
  expect(manifest.name).toBe('Лілейка');
  expect(manifest.display).toBe('standalone');
  expect(manifest.start_url).toBe('./');
  expect(manifest.scope).toBe('./');
  const sizes = manifest.icons.map((icon: { sizes: string }) => icon.sizes);
  expect(sizes).toEqual(expect.arrayContaining(['192x192', '512x512']));
  expect(manifest.icons.some((icon: { purpose: string }) => icon.purpose === 'maskable')).toBe(true);
  const responses = await Promise.all(
    manifest.icons.map((icon: { src: string }) => request.get(icon.src)),
  );
  for (const response of responses) {
    expect(response.ok(), response.url()).toBe(true);
  }
});

test('picks the platform look and shows the main tabs', async ({ page }, info) => {
  const project = info.project.name;
  const mode = project === 'iphone' ? 'ios' : 'md';

  await signIn(page);
  await expect(page.locator('html')).toHaveAttribute('mode', mode);
  await expect(page.getByText('Привіт від S1')).toBeVisible();
  await expect(page).toHaveURL(/\/m\/tabs\/home$/);
  await expect(page.getByText('вкладка браузера')).toBeVisible();
  // The API comes from the server's /env.js, like on a self-hosted install.
  await expect(page.getByText('https://api.example.test/api')).toBeVisible();
  if (project === 'iphone') {
    await expect(page.getByText('Додай Лілейку на екран')).toBeVisible();
  }
  await shot(page, project, '01-home-light');

  await page.getByRole('button', { name: 'Натиснути' }).click();
  await page.getByRole('button', { name: 'Натиснути' }).click();
  await expect(page.getByText('Натиснуто: 2')).toBeVisible();

  await page.getByText('Ще', { exact: true }).click();
  await expect(page.getByText('Про Лілейку')).toBeVisible();
  await shot(page, project, '02-more-light');

  await page.emulateMedia({ colorScheme: 'dark' });
  await page.getByText('Головна', { exact: true }).click();
  await expect(page.getByText('Привіт від S1')).toBeVisible();
  await shot(page, project, '03-home-dark');
});

test('looks like an app when opened from the home screen', async ({ page }, info) => {
  await page.emulateMedia({ colorScheme: 'light' });
  await page.addInitScript(() => {
    // What iOS and Chrome report for a home-screen launch.
    Object.defineProperty(navigator, 'standalone', { value: true });
    const original = window.matchMedia.bind(window);
    window.matchMedia = (query: string) =>
      query.includes('display-mode: standalone')
        ? ({ ...original(query), matches: true, media: query } as MediaQueryList)
        : original(query);
  });
  await signIn(page);
  await expect(page.getByText('застосунок', { exact: true })).toBeVisible();
  await expect(page.getByText('Додай Лілейку на екран')).toHaveCount(0);
  await shot(page, info.project.name, '04-home-standalone');
});

test('keeps working offline after the first visit', async ({ page, context }, info) => {
  test.skip(info.project.name !== 'android', 'Playwright drives service workers in Chromium only');

  await signIn(page);
  // Wait until ngsw has activated and prefetched the app shell into its cache.
  await expect
    .poll(
      () =>
        page.evaluate(async () => {
          await navigator.serviceWorker.ready;
          const key = (await caches.keys()).find((name) => name.endsWith(':assets:app:cache'));
          const shell = new URL('index.html', document.baseURI).pathname;
          return key !== undefined && (await (await caches.open(key)).match(shell)) !== undefined;
        }),
      { timeout: 45_000 },
    )
    .toBe(true);

  await context.setOffline(true);
  await page.reload();
  await expect(page.getByText('Привіт від S1')).toBeVisible();
  await page.getByText('Ще', { exact: true }).click();
  await expect(page.getByText('Про Лілейку')).toBeVisible();
  await shot(page, info.project.name, '05-offline');
});

test('the page does not zoom like a website', async ({ page }) => {
  await page.goto('./');
  await expect(page.getByRole('button', { name: 'Увійти' })).toBeVisible();
  const blocked = await page.evaluate(() => {
    const pinch = new Event('gesturestart', { cancelable: true });
    document.dispatchEvent(pinch);
    return {
      pinch: pinch.defaultPrevented,
      touchAction: getComputedStyle(document.documentElement).touchAction,
    };
  });
  expect(blocked.pinch).toBe(true);
  expect(blocked.touchAction).toBe('pan-x pan-y');
});

test('signs in with email and password, and keeps the session across a reload', async ({ page }, info) => {
  await mockApi(page.context());
  await page.goto('./tabs/more');
  await expect(page).toHaveURL(/\/m\/login$/);
  await shot(page, info.project.name, '00-login');

  await fillCredentials(page, { ...member, password: 'wrong' });
  await expect(page.getByRole('alert')).toHaveText('Невірний email або пароль.');
  await expect(page).toHaveURL(/\/m\/login$/);
  await shot(page, info.project.name, '00-login-error');

  await page.locator('[data-testid=password] input').fill(member.password);
  await page.getByRole('button', { name: 'Увійти' }).click();
  await expect(page.getByText(`Ти увійшов як ${member.email}`)).toBeVisible();
  const stored = await page.evaluate(() => JSON.parse(localStorage.getItem('authState') ?? '{}'));
  expect(stored).toMatchObject({ email: member.email, accessToken: null });

  // The access token lives in memory only; after a reload it comes back from the refresh cookie.
  const refreshed = page.waitForRequest((r) => r.url().endsWith('/auth/refresh') && r.method() === 'POST');
  await page.reload();
  await refreshed;
  await expect(page.getByText(`Ти увійшов як ${member.email}`)).toBeVisible();

  await page.goto('./login');
  await expect(page).toHaveURL(/\/m\/tabs\/home$/);
});

test('asks for the code when the account has two-factor sign-in', async ({ page }, info) => {
  await mockApi(page.context());
  await page.goto('./');
  await fillCredentials(page, leader);
  await expect(page.getByText('Підтвердження входу')).toBeVisible();
  await shot(page, info.project.name, '00-login-code');

  await page.locator('[data-testid=code] input').fill('000000');
  await page.getByRole('button', { name: 'Підтвердити' }).click();
  await expect(page.getByRole('alert')).toHaveText('Невірний код підтвердження.');

  await page.locator('[data-testid=code] input').fill('123456');
  await page.getByRole('button', { name: 'Підтвердити' }).click();
  await expect(page.getByText(`Ти увійшов як ${leader.email}`)).toBeVisible();
});

test('signs out from the Ще tab', async ({ page }, info) => {
  await signIn(page);
  await page.getByText('Ще', { exact: true }).click();
  await expect(page.getByText(member.email, { exact: true })).toBeVisible();
  await shot(page, info.project.name, '06-more-signed-in');

  const loggedOut = page.waitForRequest((r) => r.url().endsWith('/auth/logout'));
  await page.getByText('Вийти', { exact: true }).click();
  await loggedOut;
  await expect(page).toHaveURL(/\/m\/login$/);
  expect(await page.evaluate(() => localStorage.getItem('authState'))).toBeNull();

  await page.goto('./tabs/home');
  await expect(page).toHaveURL(/\/m\/login$/);
});
