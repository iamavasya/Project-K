import { expect, Locator, Page } from '@playwright/test';

export function dialog(page: Page): Locator {
  return page.getByRole('dialog').last();
}

export async function closeDialog(page: Page): Promise<void> {
  const activeDialog = dialog(page);
  await activeDialog.getByRole('button').first().click();
  await expect(activeDialog).toBeHidden();
}

export async function closePrimeDialog(activeDialog: Locator): Promise<void> {
  await activeDialog.locator('.p-dialog-header button').first().click();
  await expect(activeDialog).toBeHidden();
}

export async function closeLastPrimeDialog(page: Page): Promise<void> {
  const dialogs = page.getByRole('dialog');
  const dialogCount = await dialogs.count();
  await dialogs.last().locator('.p-dialog-header button').first().click();
  await expect(dialogs).toHaveCount(dialogCount - 1);
}

export async function openRowMenu(row: Locator): Promise<void> {
  await row.getByRole('button').last().click();
}

export async function chooseMenuItem(page: Page, index: number): Promise<void> {
  await page.getByRole('menuitem').nth(index).click();
}

export function visibleMenuItems(page: Page): Locator {
  return page.getByRole('menuitem');
}

export async function openGroupActionMenu(page: Page): Promise<void> {
  await page.locator('.group-actions').getByRole('button').first().click();
  await expect(visibleMenuItems(page).first()).toBeVisible();
}

export async function submitManagePanel(page: Page): Promise<void> {
  await dialog(page).locator('.actions').last().getByRole('button').last().click();
}

export async function cancelManagePanel(page: Page): Promise<void> {
  await dialog(page).locator('.actions').last().getByRole('button').first().click();
}

export async function fillManagePanelFields(page: Page, values: string[]): Promise<void> {
  const inputs = dialog(page).locator('input:visible:not([disabled]), textarea:visible:not([disabled])');
  for (let index = 0; index < values.length; index += 1) {
    await inputs.nth(index).fill(values[index]);
  }
}

export function submitButton(scope: Locator): Locator {
  return scope.getByRole('button').last();
}

export async function fillMemberRequiredFields(page: Page, data: {
  firstName: string;
  middleName: string;
  lastName: string;
  email: string;
  phone?: string;
  dateOfBirth?: string;
}): Promise<void> {
  await page.locator('input[name="firstName"]').fill(data.firstName);
  await page.locator('input[name="middleName"]').fill(data.middleName);
  await page.locator('input[name="lastName"]').fill(data.lastName);
  await page.locator('input[name="email"]').fill(data.email);

  const phoneInput = page.locator('input[name="phoneNumber"]');
  await phoneInput.click();
  await phoneInput.pressSequentially(data.phone ?? '1234567890', { delay: 50 });

  const dobInput = page.locator('input[name="dateOfBirth"]');
  await dobInput.click();
  await dobInput.fill('');
  await dobInput.pressSequentially(data.dateOfBirth ?? '2000-12-12', { delay: 50 });
  await dobInput.press('Enter');
  await page.keyboard.press('Escape');
  await dobInput.press('Tab');
}

export async function openWarningPanel(page: Page): Promise<void> {
  const warningPanel = page.locator('p-accordion-panel').filter({
    has: page.locator('#warning-Level1')
  });
  const warningSection = warningPanel.locator('.warning-section');

  if (!(await warningSection.isVisible())) {
    await warningPanel.getByRole('button').first().click();
  }

  await expect(warningSection).toBeVisible();
}

export async function acceptBrowserConfirm(page: Page): Promise<void> {
  page.once('dialog', async confirmDialog => {
    await confirmDialog.accept();
  });
}
