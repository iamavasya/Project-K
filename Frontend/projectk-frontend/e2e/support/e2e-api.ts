import { APIRequestContext, expect } from '@playwright/test';

export const e2eApiUrl = process.env.PLAYWRIGHT_API_URL ?? 'http://localhost:5205/api';
/**
 * Reset before the suite unless explicitly switched off.
 *
 * This used to be opt-in via E2E_RESET_ENABLED=true, which nothing ever set — not the local run, not
 * the workflow — so the reset silently no-opped and the database accumulated every run's data. It had
 * reached 415 active beta accounts against a cap of 10, which is enough to change what the app shows
 * and to make the onboarding flow fail in different places each time. The endpoint only exists in the
 * E2E environment and is token-protected, so defaulting it on is safe.
 */
export const e2eResetEnabled = process.env.E2E_RESET_ENABLED !== 'false';
export const e2eResetToken = process.env.E2E_RESET_TOKEN ?? 'local-e2e-reset-token';

export async function resetE2eData(request: APIRequestContext): Promise<void> {
  if (!e2eResetEnabled) {
    return;
  }

  const response = await request.post(`${e2eApiUrl}/test/e2e/reset`, {
    headers: {
      'X-E2E-Reset-Token': e2eResetToken
    }
  });

  expect(response.ok(), `E2E reset failed with ${response.status()} ${await response.text()}`).toBe(true);
}
