import { expect, test } from '@playwright/test';
import { getSeededGroupKey } from './support/api-client';
import { describeRole } from './support/role-test';
import { fillMemberRequiredFields, openRowMenu } from './support/ui';

describeRole('manager', 'Manager CRUD operations', ({ user }) => {
  test('manager can open seeded group and update description', async ({ page, request }) => {
    const groupKey = await getSeededGroupKey(request, user, 'Gurtok 1');
    const description = `E2E group description ${Date.now()}`;

    await page.goto(`/group/${groupKey}`);
    await expect(page.locator('body')).toContainText('Gurtok 1');

    await openRowMenu(page.locator('.group-actions'));
    // By name, not by index: the menu is rebuilt as each check-access answer arrives, so for a
    // moment after the page opens its first item can be «Додати учасника» instead of the edit.
    await page.getByRole('menuitem', { name: 'Редагувати профіль' }).click();
    await page.locator('textarea[formControlName="description"]').fill(description);
    await page.locator('.group-profile-actions').getByRole('button').last().click();

    await expect(page.locator('body')).toContainText(description);
  });

  test('manager can create, edit, and delete a member', async ({ page, request }) => {
    const groupKey = await getSeededGroupKey(request, user, 'Gurtok 1');
    await page.goto(`/group/${groupKey}/member/upsert`);

    const timestamp = Date.now();
    const testFirstName = `E2EFirst${timestamp}`;

    await fillMemberRequiredFields(page, {
      firstName: testFirstName,
      middleName: 'E2EMiddle',
      lastName: 'E2ELast',
      email: `e2e.member.${timestamp}@example.com`
    });

    const saveButton = page.getByRole('button', { name: 'Додати учасника', exact: true });
    await expect(saveButton).toBeEnabled();
    await saveButton.click();

    await expect(page.locator('h1')).toContainText(testFirstName, { timeout: 10000 });

    await page.locator('.member-actions button').click();

    await page.locator('input[name="middleName"]').fill('E2EMiddleEdited');
    const saveEditButton = page.getByRole('button', { name: 'Зберегти', exact: true });
    await expect(saveEditButton).toBeEnabled();
    await saveEditButton.click();

    await expect(page.locator('body')).toContainText('E2EMiddleEdited', { timeout: 10000 });

    await page.locator('.member-actions button').click();

    const deleteButton = page.getByRole('button', { name: 'Видалити профіль' });
    await expect(deleteButton).toBeEnabled();
    await deleteButton.click();

    const confirmButton = page.getByRole('button', { name: 'Видалити', exact: true });
    await expect(confirmButton).toBeVisible();
    await confirmButton.click();

    await expect(page).toHaveURL(new RegExp(`/group/${groupKey}`));
    await expect(page.locator('body')).not.toContainText(testFirstName);
  });
});
