import { randomUUID } from 'node:crypto';
import { expect, test, type BrowserContext, type Page } from '@playwright/test';

const shots = 'test-results/pwa-screens';
// What scripts/serve-www.mjs puts in /env.js.
const api = 'https://api.example.test/api';
// Mock accounts; their passwords only have to match between the form and the stand-in API.
const member = { email: 'yunak@example.com', password: randomUUID() };
// Has the second factor on: sign-in asks for the code.
const leader = { email: 'lead@example.com', password: randomUUID() };
// Провід without the second factor: must turn it on before anything else.
const admin = { email: 'admin@example.com', password: randomUUID() };
const accounts = [member, leader, admin];

const day = 86_400_000;
const soon = (days: number, hour: number) => {
  const date = new Date(Date.now() + days * day);
  date.setHours(hour, 0, 0, 0);
  return date.toISOString();
};
const kurin = { kurinKey: 'k1', kurinNumber: 7, namedAfter: null, isCurrent: true };

/** What the API answered and was asked, for assertions on writes. */
interface ApiLog {
  writes: { method: string; path: string; body: unknown }[];
}

/** One answer of the stand-in API: a JSON body, or an empty 200 for a plain-text endpoint. */
type Answer = { status: number; json: unknown } | { status: number; empty: true };

/** The refresh cookie as a flag (who it signs in), and which accounts have the second factor on. */
interface MockState {
  session: string | null;
  mfaEnabled: Set<string>;
}

const ok = (json: unknown): Answer => ({ status: 200, json });
const refuse = (status: number, error?: string): Answer => ({ status, json: error ? { error, message: error } : {} });

function signedInAs(state: MockState, email: string): Answer {
  state.session = email;
  return ok({
    userKey: `u-${email}`, memberKey: 'm1', email, isAdmin: email === admin.email, permissions: [], roles: [],
    kurinKey: 'k1', requiresMfa: false, tokens: { accessToken: 'access-1' },
  });
}

function login(state: MockState, body: Record<string, string>): Answer {
  const account = accounts.find((a) => a.email === body.email && a.password === body.password);
  if (!account) return refuse(401, 'InvalidCredentials');
  if (state.mfaEnabled.has(account.email)) return ok({ requiresMfa: true, mfaToken: 'mfa-1', tokens: null });
  return signedInAs(state, account.email);
}

/** The auth endpoints; null for anything else. */
function authAnswer(state: MockState, path: string, body: Record<string, string>, bearer: boolean): Answer | null {
  switch (path) {
    case 'auth/login':
      return login(state, body);
    case 'auth/mfa/login-verify':
      return body.code === '123456' && body.mfaToken === 'mfa-1' ? signedInAs(state, body.email) : refuse(401, 'InvalidMfaCode');
    case 'auth/refresh':
      return state.session ? ok({ accessToken: 'access-2' }) : refuse(401, 'Unauthorized');
    case 'auth/logout':
      // Like the API's [Authorize]: signing out needs the access token, not only the cookie.
      if (!bearer) return refuse(401);
      state.session = null;
      return { status: 200, empty: true };
    case 'auth/mfa/status':
      return ok({ isMfaEnabled: state.mfaEnabled.has(state.session ?? ''), isMfaRequired: state.session === admin.email });
    case 'auth/mfa/setup':
      return ok({
        sharedKey: 'JBSW Y3DP EHPK 3PXP',
        authenticatorUri: 'otpauth://totp/Lileyka:admin@example.com?secret=JBSWY3DPEHPK3PXP',
        qrCodeBase64: 'data:image/svg+xml;base64,' + Buffer.from(
          '<svg xmlns="http://www.w3.org/2000/svg" width="10" height="10"><rect width="10" height="10"/></svg>',
        ).toString('base64'),
      });
    case 'auth/mfa/enable':
      if (body.code !== '123456') return refuse(400, 'InvalidMfaCode');
      state.mfaEnabled.add(state.session ?? '');
      return ok({ enabled: true, recoveryCodes: ['aaaa-1111', 'bbbb-2222', 'cccc-3333', 'dddd-4444'], tokens: { accessToken: 'access-3' } });
    default:
      return null;
  }
}

/** The dashboard and profile reads, the person's own data as api/me returns it. */
function meAnswer(state: MockState, path: string): Answer | null {
  switch (path) {
    case 'member/m1':
      return ok({
        memberKey: 'm1', firstName: 'Остап', middleName: 'Петрович', lastName: 'Коваль', email: state.session,
        phoneNumber: '+380 67 000 00 00', dateOfBirth: '2012-03-14', groupName: 'Соколи',
        latestPlastLevelDisplay: 'Учасник', profilePhotoUrl: null,
      });
    case 'me/events':
      return ok([
        { agendaItemKey: 'e1', kurin, title: 'Сходини гуртка', startUtc: soon(1, 17), endUtc: soon(1, 19), isAllDay: false,
          isRecurring: true, location: 'Пласт-дім, Львів', categoryName: null, categoryColorHex: null, rsvpRequired: true, myResponse: null },
        { agendaItemKey: 'e2', kurin, title: 'Мандрівка на Говерлу', startUtc: soon(5, 8), endUtc: null, isAllDay: true,
          isRecurring: false, location: null, categoryName: null, categoryColorHex: null, rsvpRequired: true, myResponse: 'Maybe' },
      ]);
    case 'me/tasks':
      return ok([
        { agendaItemKey: 't1', kurin, title: 'Вивчити вузли', status: 'Todo', startUtc: null, endUtc: soon(3, 20), canChangeStatus: true },
        { agendaItemKey: 't2', kurin, title: 'Принести намет', status: 'InProgress', startUtc: null, endUtc: soon(-1, 20), canChangeStatus: true },
      ]);
    case 'me/growth':
      return ok({
        memberKey: 'm1', hasYouthProgram: true,
        probe: { probeId: 'p1', title: 'Перша проба', status: 'InProgress', signedPoints: 12, totalPoints: 30,
          nextPoints: [{ pointId: 'x1', sectionCode: '1.3', title: 'Знати Пластовий закон' }] },
        badges: { onReview: [{ badgeId: 'b1', title: 'Кухар', status: 'Submitted' }], inWork: [], confirmed: [], confirmedCount: 4 },
      });
    case 'me/score':
      return ok([{ kurin, groupKey: 'g1', groupName: 'Соколи', periodLabel: 'осінь 2026', total: 42, groupPlace: 3, groupCount: 12 }]);
    case 'me/dues':
      return ok([{ kurin, groupName: 'Соколи', quarterYear: 2026, quarterNumber: 4, balance: -150, quarterRate: 150, isConcession: false }]);
    default:
      return null;
  }
}

/**
 * A stand-in for the API: auth with the refresh cookie kept as a flag, and the dashboard reads.
 * Routed on the context so requests passing through the service worker are caught too.
 */
async function mockApi(context: BrowserContext): Promise<ApiLog> {
  const log: ApiLog = { writes: [] };
  const state: MockState = { session: null, mfaEnabled: new Set([leader.email]) };

  await context.route(`${api}/**`, (route) => {
    const request = route.request();
    const headers = {
      'access-control-allow-origin': request.headers()['origin'] ?? '*',
      'access-control-allow-credentials': 'true',
      'access-control-allow-headers': 'content-type, authorization',
      'access-control-allow-methods': 'GET, POST, PUT, OPTIONS',
    };
    if (request.method() === 'OPTIONS') return route.fulfill({ status: 204, headers });
    const path = new URL(request.url()).pathname.replace('/api/', '');
    const body = request.postDataJSON() ?? {};
    const bearer = request.headers()['authorization']?.startsWith('Bearer access-') ?? false;
    if (request.method() === 'PUT') log.writes.push({ method: 'PUT', path, body });

    // Everything past sign-in needs the token, as on the real API.
    const fallback = request.method() === 'PUT' ? ok({}) : refuse(404);
    const signedInAnswer = bearer ? (meAnswer(state, path) ?? fallback) : refuse(401);
    const answer = authAnswer(state, path, body, bearer) ?? signedInAnswer;
    return 'empty' in answer
      ? route.fulfill({ status: answer.status, headers, body: '' })
      : route.fulfill({ status: answer.status, headers, json: answer.json });
  });
  return log;
}

async function fillCredentials(page: Page, account: { email: string; password: string }): Promise<void> {
  await page.locator('[data-testid=email] input').fill(account.email);
  await page.locator('[data-testid=password] input').fill(account.password);
  await page.getByRole('button', { name: 'Увійти' }).click();
}

async function signIn(page: Page, account = member): Promise<ApiLog> {
  const log = await mockApi(page.context());
  await page.goto('./');
  await expect(page).toHaveURL(/\/m\/login$/);
  await fillCredentials(page, account);
  await expect(page).toHaveURL(/\/m\/tabs\/home$/);
  await expect(page.getByText('Сходини гуртка')).toBeVisible();
  return log;
}

async function shot(page: Page, project: string, name: string): Promise<void> {
  await page.waitForTimeout(900); // let Ionic transitions settle (the iOS 27 page motion runs ~0.6s)
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

test('shows the youth dashboard in the platform look', async ({ page }, info) => {
  const project = info.project.name;
  await signIn(page);
  await expect(page.locator('html')).toHaveAttribute('mode', project === 'iphone' ? 'ios' : 'md');
  await expect(page.getByText(/^Остап · /)).toBeVisible();
  await expect(page.getByText('Мандрівка на Говерлу')).toBeVisible();
  await expect(page.getByText('Вивчити вузли')).toBeVisible();
  await expect(page.getByText('прострочено', { exact: false })).toBeVisible();
  await expect(page.getByText('Підписано 12 з 30')).toBeVisible();
  await expect(page.getByText('42', { exact: true })).toBeVisible();
  await expect(page.getByText('Борг 150 ₴')).toBeVisible();
  if (project === 'iphone') {
    await expect(page.getByText('Додай Лілейку на екран')).toBeVisible();
  }
  await shot(page, project, '01-home-light');
  await page.evaluate(() => document.querySelector<HTMLIonContentElement>('app-home ion-content')?.scrollToBottom(0));
  await expect(page.getByText('Борг 150 ₴')).toBeInViewport();
  await shot(page, project, '01-home-light-scrolled');

  await page.emulateMedia({ colorScheme: 'dark' });
  await page.evaluate(() => document.querySelector<HTMLIonContentElement>('app-home ion-content')?.scrollToTop(0));
  await shot(page, project, '03-home-dark');
  await page.emulateMedia({ colorScheme: 'light' });
});

test('answers an event and moves a task from the dashboard', async ({ page }) => {
  const log = await signIn(page);

  const event = page.getByTestId('event').filter({ hasText: 'Сходини гуртка' });
  await event.locator('ion-segment-button').filter({ hasText: /^Іду$/ }).click();
  await expect.poll(() => log.writes.find((w) => w.path === 'me/events/e1/response')?.body).toMatchObject({
    status: 'Going',
  });
  // A series is answered per occurrence.
  expect((log.writes.find((w) => w.path === 'me/events/e1/response')?.body as { occurrenceStartUtc: string })
    .occurrenceStartUtc).toBeTruthy();

  const task = page.getByTestId('task').filter({ hasText: 'Вивчити вузли' });
  await task.getByRole('button', { name: 'Почати' }).click();
  await expect(task.getByRole('button', { name: 'Зроблено' })).toBeVisible();
  expect(log.writes.find((w) => w.path === 'agenda/t1/status')?.body).toEqual({ status: 'InProgress' });

  await task.getByRole('button', { name: 'Зроблено' }).click();
  await expect(page.getByTestId('task').filter({ hasText: 'Вивчити вузли' })).toHaveCount(0);
});

test('shows the profile and the app details from Ще', async ({ page }, info) => {
  const project = info.project.name;
  await signIn(page);
  await page.getByText('Ще', { exact: true }).click();
  await expect(page.getByText('вимкнено', { exact: true })).toBeVisible();
  await shot(page, project, '02-more-light');

  // The account row on top, as in iOS Settings: the person's name, opening the profile.
  await expect(page.getByTestId('account')).toContainText('Остап Коваль');
  await page.getByTestId('account').click();
  await expect(page.locator('app-profile').getByText('Остап Коваль')).toBeVisible();
  await expect(page.getByText('Коваль Остап Петрович')).toBeVisible();
  await expect(page.getByText('14 березня 2012')).toBeVisible();
  await shot(page, project, '07-profile');

  await page.goto('./tabs/more/about');
  await expect(page.getByText('вкладка браузера')).toBeVisible();
  // The API comes from the server's /env.js, like on a self-hosted install.
  await expect(page.getByText(api)).toBeVisible();
  await page.getByRole('button', { name: 'Натиснути' }).click();
  await page.getByRole('button', { name: 'Натиснути' }).click();
  await expect(page.getByText('Натиснуто: 2')).toBeVisible();
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
  await expect(page.getByText('Додай Лілейку на екран')).toHaveCount(0);
  await shot(page, info.project.name, '04-home-standalone');
  await page.goto('./tabs/more/about');
  await expect(page.getByText('застосунок', { exact: true })).toBeVisible();
});

test.describe('with the service worker', () => {
  test.use({ serviceWorkers: 'allow' });

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
    await expect(page.getByText('Найближче', { exact: true })).toBeVisible();
    await page.getByText('Ще', { exact: true }).click();
    await expect(page.getByText('Про застосунок')).toBeVisible();
    await shot(page, info.project.name, '05-offline');
  });
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
  await expect(page.getByText('Сходини гуртка')).toBeVisible();
  const stored = await page.evaluate(() => JSON.parse(localStorage.getItem('authState') ?? '{}'));
  expect(stored).toMatchObject({ email: member.email, accessToken: null });

  // The access token lives in memory only; after a reload it comes back from the refresh cookie.
  const refreshed = page.waitForRequest((r) => r.url().endsWith('/auth/refresh') && r.method() === 'POST');
  await page.reload();
  await refreshed;
  await expect(page.getByText('Сходини гуртка')).toBeVisible();

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
  await expect(page.getByText('Сходини гуртка')).toBeVisible();
});

test('makes провід turn on two-factor sign-in before anything else', async ({ page }, info) => {
  const project = info.project.name;
  await mockApi(page.context());
  await page.goto('./');
  await fillCredentials(page, admin);
  await expect(page).toHaveURL(/\/m\/mfa$/);
  await expect(page.getByText('двофакторний вхід обовʼязковий', { exact: false })).toBeVisible();
  await expect(page.getByTestId('shared-key')).toHaveText('JBSW Y3DP EHPK 3PXP');
  await shot(page, project, '08-mfa-setup');

  // No way around it: the tabs send the person back.
  await page.goto('./tabs/home');
  await expect(page).toHaveURL(/\/m\/mfa$/);

  await page.locator('[data-testid=mfa-code] input').filter({ visible: true }).fill('000000');
  await page.getByRole('button', { name: 'Увімкнути' }).click();
  await expect(page.getByRole('alert')).toHaveText('Код не підійшов. Перевір застосунок і спробуй ще раз.');

  await page.locator('[data-testid=mfa-code] input').filter({ visible: true }).fill('123456');
  await page.getByRole('button', { name: 'Увімкнути' }).click();
  await expect(page.getByTestId('recovery-codes')).toContainText('aaaa-1111');
  await shot(page, project, '09-mfa-codes');

  await page.getByRole('button', { name: 'Я зберіг коди' }).click();
  await expect(page).toHaveURL(/\/m\/tabs\/home$/);
  await expect(page.getByText('Сходини гуртка')).toBeVisible();
});

test('signs out from the Ще tab', async ({ page }, info) => {
  await signIn(page);
  await page.getByText('Ще', { exact: true }).click();
  await expect(page.getByTestId('account')).toContainText(member.email);

  // A sheet asks first; backing out keeps the session.
  await page.locator('app-more').getByText('Вийти', { exact: true }).click();
  await page.getByRole('button', { name: 'Скасувати' }).click();
  await expect(page.locator('ion-action-sheet')).toHaveCount(0);
  expect(await page.evaluate(() => localStorage.getItem('authState'))).not.toBeNull();

  const loggedOut = page.waitForResponse((r) => r.url().endsWith('/auth/logout'));
  await page.locator('app-more').getByText('Вийти', { exact: true }).click();
  await shot(page, info.project.name, '11-sign-out-sheet');
  await page.locator('ion-action-sheet').getByRole('button', { name: 'Вийти' }).click();
  expect((await loggedOut).ok()).toBe(true);
  await expect(page).toHaveURL(/\/m\/login$/);
  expect(await page.evaluate(() => localStorage.getItem('authState'))).toBeNull();

  await page.goto('./tabs/home');
  await expect(page).toHaveURL(/\/m\/login$/);
});
