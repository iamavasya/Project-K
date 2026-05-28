import { APIRequestContext, expect } from '@playwright/test';

export const e2eApiUrl = process.env.PLAYWRIGHT_API_URL ?? 'http://localhost:5205/api';
export const e2eResetEnabled = process.env.E2E_RESET_ENABLED === 'true';
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
