// Знімає скриншоти демо-куреня для довідки (docs/user/images/*.png) з dev-стеку на 4200.
// Запуск з Frontend/projectk-frontend: node scripts/capture-docs-screenshots.mjs
// Потрібен запущений `./scripts/dev.sh up dev` і демо-дані (Development). Світла тема, 1280×800,
// дев-перемикач ролей прихований. Сторінки куреня знімаються Звʼязковим демо-куреня (demo0),
// адмін-панель — адміністратором.
import { chromium } from 'playwright';
import { mkdirSync } from 'node:fs';
import { resolve } from 'node:path';

const base = process.env.LILEYKA_URL ?? 'http://localhost:4200';
const api = process.env.LILEYKA_API ?? 'http://localhost:5205/api';
const out = resolve('../../docs/user/images');
mkdirSync(out, { recursive: true });

const browser = await chromium.launch();
const context = await browser.newContext({ viewport: { width: 1280, height: 800 }, deviceScaleFactor: 1 });
const page = await context.newPage();
page.setDefaultTimeout(20000);

async function shot(name) {
	await page.addStyleTag({ content: '.dev-switch { display: none !important; }' });
	await page.waitForTimeout(1200);
	await page.screenshot({ path: resolve(out, `${name}.png`) });
	console.log('✓', name);
}

async function goto(path) {
	await page.goto(base + path, { waitUntil: 'networkidle' });
}

async function signIn(email, password) {
	await goto('/login');
	await page.evaluate(() => {
		localStorage.setItem('lileyka-theme', 'light');
		localStorage.removeItem('authState');
	});
	await goto('/login');
	await page.fill('input#email', email);
	await page.fill('input[type=password]', password);
	await page.click('button[type=submit]');
	await page.waitForURL(/\/(panel|kurin)/);
	await page.waitForLoadState('networkidle');
}

async function signOut() {
	await page.evaluate(async (apiUrl) => {
		const refresh = await fetch(`${apiUrl}/auth/refresh`, { method: 'POST', credentials: 'include' });
		const token = (await refresh.json()).accessToken;
		await fetch(`${apiUrl}/auth/logout`, { method: 'POST', credentials: 'include', headers: { Authorization: `Bearer ${token}` } });
		localStorage.removeItem('authState');
		sessionStorage.clear();
	}, api);
}

/** Один запит до API від імені поточної сесії (токен беремо через refresh-cookie). */
async function apiGet(path) {
	return page.evaluate(async ([apiUrl, p]) => {
		const refresh = await fetch(`${apiUrl}/auth/refresh`, { method: 'POST', credentials: 'include' });
		const token = (await refresh.json()).accessToken;
		const res = await fetch(`${apiUrl}${p}`, { headers: { Authorization: `Bearer ${token}` } });
		return res.json();
	}, [api, path]);
}

// Публічні сторінки
await goto('/login');
await page.evaluate(() => localStorage.setItem('lileyka-theme', 'light'));
await goto('/login');
await shot('login');
await goto('/join');
await shot('join');

// Адміністратор: панель
await signIn('admin@projectk.com', 'Admin@12345');
await goto('/panel');
await shot('admin-panel');
await signOut();

// Звʼязковий демо-куреня: усе інше
await signIn('demo0@projectk.com', 'User@12345');
await goto('/kurin');
await shot('kurin');
const kurinKey = await page.evaluate(() => JSON.parse(localStorage.getItem('authState') ?? '{}').kurinKey);

await goto('/kurin/registry');
await shot('registry');

const groups = await apiGet(`/group/groups?kurinKey=${kurinKey}`);
if (Array.isArray(groups) && groups.length) {
	await goto(`/group/${groups[0].groupKey}`);
	await shot('group');
}

await goto('/kurin');
await page.locator('.member-list-table tbody tr').first().click();
await page.waitForURL(/\/member\//);
await page.waitForLoadState('networkidle');
await shot('member-card');

const details = page.locator('button:has-text("Деталі")').first();
if (await details.count()) {
	await details.click();
	await page.waitForURL(/\/probe\//);
	await page.waitForLoadState('networkidle');
	await shot('probe');
}

await goto(`/calendar/${kurinKey}`);
await shot('calendar');
await goto(`/tasks/${kurinKey}`);
await shot('tasks');
await goto(`/planning/${kurinKey}`);
await shot('planning');
await goto(`/kurin/${kurinKey}/review/skills`);
await shot('skills-review');
await goto(`/kurin/${kurinKey}/settings`);
await shot('kurin-settings');
await goto('/kurin/import');
await shot('import');
await goto('/settings/account');
await shot('account');

await browser.close();
console.log('done →', out);
