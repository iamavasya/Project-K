import { expect, test } from '@playwright/test';

import {
  getKurinScopeOptions,
  getMembershipsViaApi,
  setKurinScopeViaApi
} from './support/api-client';
import { describeRole } from './support/role-test';

/**
 * Одна людина у двох куренях — те, заради чого робився 0.20.0.
 *
 * Виховник куреня 2 має друге, звичайне членство в курені 3. Тут перевіряється, що переміщення між
 * ними міняє права, що історія від цього нікуди не дівається, і що перемикач з'являється саме тому,
 * що куренів більше одного.
 */
// Serial on purpose. Which kurin the session stands in is server-side state on the account
// (`AppUser.ActiveKurinKey`), and all three tests here drive the same account — run in parallel they
// switch the scope out from under each other, and the failure looks like a flake rather than a race.
test.describe.configure({ mode: 'serial' });

describeRole('mentor', 'A person who belongs to two kurins', ({ user }) => {
  // Scoping is a write to the account, and the rest of the suite expects this mentor in the kurin
  // where their office is. Leaving them in the second one made ten unrelated specs fail with what
  // looked like missing permissions.
  test.afterAll(async ({ request }) => {
    const { accessToken, options } = await getKurinScopeOptions(request, user);
    const home = options.find(option => option.kurinNumber === 2);
    if (home) {
      await setKurinScopeViaApi(request, accessToken, home.kurinKey);
    }
  });

  test('the toolbar offers a switch only because there is more than one kurin', async ({ page, request }) => {
    const { options } = await getKurinScopeOptions(request, user);
    expect(options.length, 'the mentor fixture is expected to belong to two kurins').toBeGreaterThan(1);

    await page.goto('/kurin');

    const switcher = page.locator('app-kurin-switcher');
    await expect(switcher.getByRole('button').first()).toBeVisible();

    // Список пропонує рівно ті курені, які дозволить сервер — а не всі, що існують.
    const numbers = options.map(option => option.kurinNumber).sort((left, right) => left - right);
    expect(numbers).toEqual([2, 3]);
  });

  test('rights follow the kurin, not the account', async ({ request }) => {
    const { accessToken, options } = await getKurinScopeOptions(request, user);

    const withOffice = options.find(option => option.kurinNumber === 2)!;
    const withoutOffice = options.find(option => option.kurinNumber === 3)!;

    const inHomeKurin = await setKurinScopeViaApi(request, accessToken, withOffice.kurinKey);
    expect(inHomeKurin.roles, 'виховник tenure lives in kurin 2').toContain('KV.Vykhovnyk');

    const inSecondKurin = await setKurinScopeViaApi(request, inHomeKurin.accessToken, withoutOffice.kurinKey);
    expect(
      inSecondKurin.roles,
      'the same account holds no office in the second kurin and must not carry the rights there'
    ).not.toContain('KV.Vykhovnyk');
  });

  test('their history names both kurins, whichever one they are standing in', async ({ request }) => {
    const { accessToken, memberKey, options } = await getKurinScopeOptions(request, user);

    const seenFrom = async (kurinKey: string) => {
      const scoped = await setKurinScopeViaApi(request, accessToken, kurinKey);
      const memberships = await getMembershipsViaApi(request, scoped.accessToken, memberKey);
      return memberships
        .filter(membership => membership.isCurrent)
        .map(membership => membership.kurinNumber)
        .sort((left, right) => left - right);
    };

    for (const option of options) {
      expect(
        await seenFrom(option.kurinKey),
        `standing in kurin ${option.kurinNumber} must not hide the other one`
      ).toEqual([2, 3]);
    }
  });
});
