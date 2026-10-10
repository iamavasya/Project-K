import { expect, test, type Page } from '@playwright/test';

const shots = 'test-results/pwa-screens';

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

  await page.goto('./');
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
  await page.goto('./');
  await expect(page.getByText('застосунок', { exact: true })).toBeVisible();
  await expect(page.getByText('Додай Лілейку на екран')).toHaveCount(0);
  await shot(page, info.project.name, '04-home-standalone');
});

test('keeps working offline after the first visit', async ({ page, context }, info) => {
  test.skip(info.project.name !== 'android', 'Playwright drives service workers in Chromium only');

  await page.goto('./');
  await expect(page.getByText('Привіт від S1')).toBeVisible();
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
  await expect(page.getByText('Привіт від S1')).toBeVisible();
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
