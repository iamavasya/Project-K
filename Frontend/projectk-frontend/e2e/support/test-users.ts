export type E2eRole = 'admin' | 'manager' | 'mentor' | 'member';

export interface E2eUser {
  role: E2eRole;
  email: string;
  password: string;
}

const defaultPassword = process.env.E2E_DEFAULT_PASSWORD ?? 'User@12345';

export const e2eUsers: Record<E2eRole, E2eUser> = {
  admin: {
    role: 'admin',
    email: process.env.E2E_ADMIN_EMAIL ?? 'admin@projectk.com',
    password: process.env.E2E_ADMIN_PASSWORD ?? 'Admin@12345'
  },
  manager: {
    role: 'manager',
    email: process.env.E2E_MANAGER_EMAIL ?? 'manager1@projectk.com',
    password: process.env.E2E_MANAGER_PASSWORD ?? defaultPassword
  },
  mentor: {
    role: 'mentor',
    email: process.env.E2E_MENTOR_EMAIL ?? 'mentor1@projectk.com',
    password: process.env.E2E_MENTOR_PASSWORD ?? defaultPassword
  },
  member: {
    role: 'member',
    email: process.env.E2E_MEMBER_EMAIL ?? 'g1member1@projectk.com',
    password: process.env.E2E_MEMBER_PASSWORD ?? defaultPassword
  }
};

export function authStatePath(role: E2eRole): string {
  return `e2e/.auth/${role}.json`;
}
