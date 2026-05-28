import { expect, test } from '@playwright/test';

import { describeRole } from './support/role-test';

test.describe('Public onboarding', () => {
  test('join form gates submission and sanitizes kurin number', async ({ page }) => {
    await page.goto('/join');
    const submit = page.locator('form').getByRole('button').last();
    await expect(submit).toBeDisabled();

    await page.locator('#firstName').fill('E2E');
    await page.locator('#lastName').fill('Applicant');
    await page.locator('#email').fill(`e2e.applicant.${Date.now()}@example.com`);
    await page.locator('#stanytsia').fill('E2E Stanytsia');
    await page.locator('#regionOrCountry').fill('E2E Region');
    await page.locator('#phone input').pressSequentially('501112233', { delay: 30 });
    const dateOfBirth = page.locator('#dob input');
    await dateOfBirth.click();
    await dateOfBirth.fill('01.01.2000');
    await dateOfBirth.fill('');
    await dateOfBirth.pressSequentially('01.01.2000', { delay: 30 });
    await dateOfBirth.press('Enter');
    await dateOfBirth.press('Tab');
    await expect(submit).toBeDisabled();

    await page.locator('#leader').check();
    await page.locator('#kurin').fill('12abc');
    await expect(page.locator('#kurin')).toHaveValue('12');
    await expect(submit).toBeEnabled();
  });

  test('invalid activation token shows recovery path back to login', async ({ page }) => {
    await page.goto(`/activate/not-a-real-token-${Date.now()}`);
    await expect(page.getByText('Invalid or Expired Invitation')).toBeVisible();
    await page.getByRole('button', { name: 'Back to Login' }).click();
    await expect(page).toHaveURL(/\/login/);
  });
});

describeRole('manager', 'Account settings', () => {
  test('settings page validates password and mfa controls without mutating account', async ({ page }) => {
    await page.goto('/settings/account');
    await expect(page.locator('.settings-header')).toBeVisible();

    const passwordForm = page.locator('.settings-form').nth(1);
    const changePassword = passwordForm.getByRole('button').last();
    await expect(changePassword).toBeDisabled();

    await page.locator('#password-current').fill('User@12345');
    await page.locator('#password-new').fill('Another@12345');
    await page.locator('#password-confirm').fill('Mismatch@12345');
    await expect(changePassword).toBeDisabled();

    await page.locator('#password-confirm').fill('Another@12345');
    await expect(changePassword).toBeEnabled();
    await expect(page.locator('.mfa-panel')).toBeVisible();
  });

  test('logout action clears session and returns to login', async ({ page }) => {
    await page.goto('/kurin');
    await page.locator('app-logout-component button').click();
    await expect(page).toHaveURL(/\/login/, { timeout: 10000 });
  });
});
