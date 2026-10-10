import { randomUUID } from 'node:crypto';
import { expect, type BrowserContext, type Page } from '@playwright/test';

/**
 * The stand-in API and sign-in steps every PWA spec shares. A feature spec answers its own
 * endpoints through `extra` and keeps its fixtures next to its tests.
 */
export const shots = 'test-results/pwa-screens';
// What scripts/serve-www.mjs puts in /env.js.
export const api = 'https://api.example.test/api';
// Mock accounts; their passwords only have to match between the form and the stand-in API.
export const member = { email: 'yunak@example.com', password: randomUUID() };
// Has the second factor on: sign-in asks for the code.
export const leader = { email: 'lead@example.com', password: randomUUID() };
// Провід without the second factor: must turn it on before anything else.
export const admin = { email: 'admin@example.com', password: randomUUID() };
export const accounts = [member, leader, admin];

export const day = 86_400_000;
export const soon = (days: number, hour: number) => {
  const date = new Date(Date.now() + days * day);
  date.setHours(hour, 0, 0, 0);
  return date.toISOString();
};
export const kurin = { kurinKey: 'k1', kurinNumber: 7, namedAfter: null, isCurrent: true };

/** What the API answered and was asked, for assertions on writes. */
export interface ApiLog {
  writes: { method: string; path: string; body: any }[];
}

/** One answer of the stand-in API: a JSON body, or an empty 200 for a plain-text endpoint. */
export type Answer = { status: number; json: unknown } | { status: number; empty: true };

/** The refresh cookie as a flag (who it signs in), and which accounts have the second factor on. */
export interface MockState {
  session: string | null;
  mfaEnabled: Set<string>;
}

export const ok = (json: unknown): Answer => ({ status: 200, json });
export const refuse = (status: number, error?: string): Answer => ({ status, json: error ? { error, message: error } : {} });

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
/**
 * Extra endpoints a feature spec answers, tried before the shared ones; null passes the request on.
 * Writes (POST/PUT/PATCH/DELETE past sign-in) land in the log either way.
 */
export type ExtraApi = (request: { method: string; path: string; query: URLSearchParams; body: any; state: MockState }) => Answer | null;

export async function mockApi(context: BrowserContext, extra?: ExtraApi): Promise<ApiLog> {
  const log: ApiLog = { writes: [] };
  const state: MockState = { session: null, mfaEnabled: new Set([leader.email]) };

  await context.route(`${api}/**`, (route) => {
    const request = route.request();
    const headers = {
      'access-control-allow-origin': request.headers()['origin'] ?? '*',
      'access-control-allow-credentials': 'true',
      'access-control-allow-headers': 'content-type, authorization',
      'access-control-allow-methods': 'GET, POST, PUT, PATCH, DELETE, OPTIONS',
    };
    if (request.method() === 'OPTIONS') return route.fulfill({ status: 204, headers });
    const url = new URL(request.url());
    const path = url.pathname.replace('/api/', '');
    const method = request.method();
    let body: any = {};
    try {
      body = request.postDataJSON() ?? {};
    } catch {
      body = request.postData() ?? {};
    }
    const bearer = request.headers()['authorization']?.startsWith('Bearer access-') ?? false;
    const write = method !== 'GET';
    if (write && !path.startsWith('auth/')) log.writes.push({ method, path, body });

    // Everything past sign-in needs the token, as on the real API.
    const fallback = write ? ok({}) : refuse(404);
    const own = bearer ? extra?.({ method, path, query: url.searchParams, body, state }) : null;
    const signedInAnswer = bearer ? (own ?? meAnswer(state, path) ?? fallback) : refuse(401);
    const answer = authAnswer(state, path, body, bearer) ?? signedInAnswer;
    return 'empty' in answer
      ? route.fulfill({ status: answer.status, headers, body: '' })
      : route.fulfill({ status: answer.status, headers, json: answer.json });
  });
  return log;
}

export async function fillCredentials(page: Page, account: { email: string; password: string }): Promise<void> {
  await page.locator('[data-testid=email] input').fill(account.email);
  await page.locator('[data-testid=password] input').fill(account.password);
  await page.getByRole('button', { name: 'Увійти' }).click();
}

export async function signIn(page: Page, account = member, extra?: ExtraApi): Promise<ApiLog> {
  const log = await mockApi(page.context(), extra);
  await page.goto('./');
  await expect(page).toHaveURL(/\/m\/login$/);
  await fillCredentials(page, account);
  await expect(page).toHaveURL(/\/m\/tabs\/home$/);
  await expect(page.getByText('Сходини гуртка')).toBeVisible();
  return log;
}

export async function shot(page: Page, project: string, name: string): Promise<void> {
  await page.waitForTimeout(900); // let Ionic transitions settle (the iOS 27 page motion runs ~0.6s)
  await page.screenshot({ path: `${shots}/${project}-${name}.png`, scale: 'css' });
}

