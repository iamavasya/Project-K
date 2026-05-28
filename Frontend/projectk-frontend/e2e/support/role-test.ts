import { test as base } from '@playwright/test';

import { interceptAuthRefresh } from './api-client';
import { authStatePath, E2eRole, e2eUsers, E2eUser } from './test-users';

interface RoleDescribeContext {
  role: E2eRole;
  user: E2eUser;
}

export function describeRole(
  role: E2eRole,
  title: string,
  defineTests: (context: RoleDescribeContext) => void
): void {
  base.describe(title, () => {
    base.use({ storageState: authStatePath(role) });

    base.beforeEach(async ({ page, request }) => {
      await interceptAuthRefresh(page, request, e2eUsers[role]);
    });

    defineTests({ role, user: e2eUsers[role] });
  });
}
