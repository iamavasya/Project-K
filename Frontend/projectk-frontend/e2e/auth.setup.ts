import { test } from '@playwright/test';
import { mkdir } from 'node:fs/promises';
import { authStatePath, e2eUsers } from './support/test-users';
import { loginThroughUi } from './support/login';
import { resetE2eData } from './support/e2e-api';

test('authenticate seeded users', async ({ browser, request }) => {
  await mkdir('e2e/.auth', { recursive: true });
  await resetE2eData(request);

  for (const user of Object.values(e2eUsers)) {
    const page = await browser.newPage();
    await loginThroughUi(page, user);
    await page.context().storageState({ path: authStatePath(user.role) });
    await page.close();
  }
});
