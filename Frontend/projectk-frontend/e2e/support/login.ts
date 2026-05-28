import { expect, Page } from '@playwright/test';
import { E2eUser } from './test-users';

export async function loginThroughUi(page: Page, user: E2eUser): Promise<void> {
  await page.goto('/login');
  await page.locator('#email').fill(user.email);
  await page.locator('#password').fill(user.password);
  await page.locator('button[type="submit"]').click();

  await expect.poll(
    async () => page.evaluate(() => JSON.parse(localStorage.getItem('authState') ?? 'null')?.email ?? null),
    { message: `Expected ${user.email} to be stored in authState after login.` }
  ).toBe(user.email);
}

export async function expectAuthenticatedAs(page: Page, user: E2eUser): Promise<void> {
  await expect.poll(
    async () => page.evaluate(() => JSON.parse(localStorage.getItem('authState') ?? 'null')?.email ?? null)
  ).toBe(user.email);
}
