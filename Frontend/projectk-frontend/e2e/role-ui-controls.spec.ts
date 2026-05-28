import { expect, test } from '@playwright/test';

import { getFirstSeededGroupMemberKey, getSeededGroupKey, getSeededKurinKey } from './support/api-client';
import { describeRole } from './support/role-test';
import { fillMemberRequiredFields, openWarningPanel } from './support/ui';

describeRole('manager', 'Manager role UI controls', ({ user }) => {
  test('manager sees mutating controls and member form validation gates save', async ({ page, request }) => {
    const groupKey = await getSeededGroupKey(request, user, 'Gurtok 1');

    await page.goto('/kurin');
    await expect(page.locator('.edit-profile-button')).toBeVisible();
    await expect(page.locator('.table-caption').getByRole('button')).toBeVisible();
    await expect(page.locator('app-member-list + div').getByRole('button')).toBeVisible();

    await page.goto(`/group/${groupKey}`);
    await expect(page.locator('.group-actions button')).toBeVisible();

    await page.goto(`/group/${groupKey}/member/upsert`);
    const saveButton = page.getByRole('button', { name: 'Create Member' });
    await expect(saveButton).toBeDisabled();

    await page.locator('input[name="firstName"]').fill('E2EValidationFirst');
    await page.locator('input[name="middleName"]').fill('E2EValidationMiddle');
    await page.locator('input[name="lastName"]').fill('E2EValidationLast');
    await page.locator('input[name="email"]').fill('not-an-email');
    await expect(saveButton).toBeDisabled();

    await fillMemberRequiredFields(page, {
      firstName: 'E2EValidationFirst',
      middleName: 'E2EValidationMiddle',
      lastName: 'E2EValidationLast',
      email: `e2e.validation.${Date.now()}@example.com`
    });

    await expect(saveButton).toBeEnabled();
  });

  test('manager warning checkboxes unlock in order on member edit', async ({ page, request }) => {
    const memberKey = await getFirstSeededGroupMemberKey(request, user);

    await page.goto(`/member/${memberKey}`);
    await expect(page.locator('.member-actions').getByRole('button')).toBeVisible();
    await page.locator('.member-actions button').click();

    const level1 = page.locator('#warning-Level1');
    const level2 = page.locator('#warning-Level2');
    const level3 = page.locator('#warning-Level3');

    await openWarningPanel(page);
    await expect(level1).toBeVisible();
    await expect(level3).toBeDisabled();

    if (await level2.isDisabled()) {
      await level1.check();
      await expect(level2).toBeEnabled();
      await level2.check();
      await expect(level3).toBeEnabled();
    }
  });
});

describeRole('mentor', 'Mentor role UI controls', ({ user }) => {
  test('mentor can review skills but does not see member edit controls', async ({ page, request }) => {
    const kurinKey = await getSeededKurinKey(request, user);
    const memberKey = await getFirstSeededGroupMemberKey(request, user);

    await page.goto(`/member/${memberKey}`);
    await expect(page.locator('.member-actions')).toBeHidden();

    await page.goto(`/kurin/${kurinKey}/review/skills`);
    await expect(page).toHaveURL(new RegExp(`/kurin/${kurinKey}/review/skills`));
    await expect(page.locator('.skills-review-page__title')).toBeVisible();
    await expect(page.locator('.p-message-warn')).toBeHidden();
  });
});

describeRole('member', 'Member role UI controls', ({ user }) => {
  test('member is restricted from skill moderation controls', async ({ page, request }) => {
    const kurinKey = await getSeededKurinKey(request, user);
    const memberKey = await getFirstSeededGroupMemberKey(request, user);

    await page.goto(`/member/${memberKey}`);
    await expect(page.locator('.member-actions')).toBeHidden();

    await page.goto(`/kurin/${kurinKey}/review/skills`);
    await expect(page).toHaveURL(new RegExp(`/kurin/${kurinKey}/review/skills`));
    await expect(page.locator('.p-message-warn')).toBeVisible();
    await expect(page.locator('.skills-review-page__row-actions')).toBeHidden();
  });
});
