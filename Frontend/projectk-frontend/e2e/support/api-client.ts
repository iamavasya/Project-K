import { APIRequestContext, expect, Page } from '@playwright/test';
import { e2eApiUrl, e2eResetToken } from './e2e-api';
import { E2eUser } from './test-users';

interface LoginResponse {
  tokens?: {
    accessToken?: string;
  };
  kurinKey?: string;
  memberKey?: string;
  role?: string;
  userKey?: string;
}

interface GroupResponse {
  groupKey: string;
  name: string;
}

interface MemberResponse {
  memberKey: string;
  userKey?: string | null;
  userRole?: string | null;
  firstName?: string;
  middleName?: string;
  lastName?: string;
  email?: string;
}

interface PlanningSessionResponse {
  planningSessionKey: string;
  name: string;
}

interface InvitationResponse {
  invitationKey: string;
  token: string;
  waitlistEntryKey: string;
  targetUserKey?: string | null;
  email: string;
}

interface KurinResponse {
  kurinKey: string;
  number: number;
}

export interface CreatedMember {
  memberKey: string;
  userKey?: string | null;
  email: string;
  firstName: string;
  middleName: string;
  lastName: string;
}

export interface CreatedGroup {
  groupKey: string;
  name: string;
}

export async function loginViaApi(request: APIRequestContext, user: E2eUser): Promise<LoginResponse> {
  const response = await request.post(`${e2eApiUrl}/auth/login`, {
    data: {
      email: user.email,
      password: user.password
    }
  });

  expect(response.ok(), `API login failed for ${user.email}: ${response.status()} ${await response.text()}`).toBe(true);

  return await response.json() as LoginResponse;
}

export async function interceptAuthRefresh(page: Page, request: APIRequestContext, user: E2eUser): Promise<void> {
  const login = await loginViaApi(request, user);
  await page.route('**/api/auth/refresh', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ accessToken: login.tokens?.accessToken })
    });
  });
}

export async function getLatestInvitationByEmail(request: APIRequestContext, email: string): Promise<InvitationResponse> {
  const response = await request.get(`${e2eApiUrl}/test/e2e/invitations/by-email`, {
    headers: {
      'X-E2E-Reset-Token': e2eResetToken
    },
    params: {
      email
    }
  });

  expect(response.ok(), `Failed to load E2E invitation for ${email}: ${response.status()} ${await response.text()}`).toBe(true);
  return await response.json() as InvitationResponse;
}

export async function activateAccount(request: APIRequestContext, token: string, password: string): Promise<void> {
  const response = await request.post(`${e2eApiUrl}/auth/onboarding/activate`, {
    data: {
      token,
      password
    }
  });

  expect(response.ok(), `Account activation failed: ${response.status()} ${await response.text()}`).toBe(true);
}

export async function getSeededKurinKey(request: APIRequestContext, user: E2eUser): Promise<string> {
  const login = await loginViaApi(request, user);
  expect(login.kurinKey, `API login for ${user.email} did not return a kurinKey.`).toBeTruthy();
  return login.kurinKey!;
}

export async function getKurinByKey(request: APIRequestContext, user: E2eUser, kurinKey: string): Promise<KurinResponse> {
  const login = await loginViaApi(request, user);
  expect(login.tokens?.accessToken, `API login for ${user.email} did not return an access token.`).toBeTruthy();

  const response = await request.get(`${e2eApiUrl}/kurin/${kurinKey}`, {
    headers: {
      Authorization: `Bearer ${login.tokens!.accessToken}`
    }
  });

  expect(response.ok(), `Failed to load kurin ${kurinKey}: ${response.status()} ${await response.text()}`).toBe(true);
  return await response.json() as KurinResponse;
}

export async function createGroupViaApi(
  request: APIRequestContext,
  user: E2eUser,
  kurinKey: string,
  name: string
): Promise<CreatedGroup> {
  const login = await loginViaApi(request, user);
  expect(login.tokens?.accessToken, `API login for ${user.email} did not return an access token.`).toBeTruthy();

  const response = await request.post(`${e2eApiUrl}/group`, {
    headers: {
      Authorization: `Bearer ${login.tokens!.accessToken}`
    },
    data: {
      groupKey: '00000000-0000-0000-0000-000000000000',
      kurinKey,
      name
    }
  });

  expect(response.ok(), `Failed to create group ${name}: ${response.status()} ${await response.text()}`).toBe(true);
  const group = await response.json() as GroupResponse;
  return { groupKey: group.groupKey, name: group.name };
}

export async function createMemberViaApi(
  request: APIRequestContext,
  user: E2eUser,
  scope: { groupKey: string } | { kurinKey: string },
  member: {
    firstName: string;
    middleName: string;
    lastName: string;
    email: string;
    createUserAccount?: boolean;
  }
): Promise<CreatedMember> {
  const login = await loginViaApi(request, user);
  expect(login.tokens?.accessToken, `API login for ${user.email} did not return an access token.`).toBeTruthy();

  const isGroupScope = 'groupKey' in scope;
  const url = isGroupScope
    ? `${e2eApiUrl}/member`
    : `${e2eApiUrl}/member/kurins/${scope.kurinKey}/members`;

  const multipart = new FormData();
  multipart.append('firstName', member.firstName);
  multipart.append('middleName', member.middleName);
  multipart.append('lastName', member.lastName);
  multipart.append('email', member.email);
  multipart.append('phoneNumber', '123-456-7890');
  multipart.append('dateOfBirth', '2000-01-01');
  multipart.append('createUserAccount', String(member.createUserAccount ?? false));

  if (isGroupScope) {
    multipart.append('groupKey', scope.groupKey);
  }

  const response = await request.post(url, {
    headers: {
      Authorization: `Bearer ${login.tokens!.accessToken}`
    },
    multipart
  });

  expect(response.ok(), `Failed to create member ${member.email}: ${response.status()} ${await response.text()}`).toBe(true);
  const savedMember = await response.json() as MemberResponse;

  return {
    memberKey: savedMember.memberKey,
    userKey: savedMember.userKey,
    email: member.email,
    firstName: member.firstName,
    middleName: member.middleName,
    lastName: member.lastName
  };
}

export async function getMembersByKurin(
  request: APIRequestContext,
  user: E2eUser,
  kurinKey: string
): Promise<MemberResponse[]> {
  const login = await loginViaApi(request, user);
  expect(login.tokens?.accessToken, `API login for ${user.email} did not return an access token.`).toBeTruthy();

  const response = await request.get(`${e2eApiUrl}/member/kurins/${kurinKey}/members`, {
    headers: {
      Authorization: `Bearer ${login.tokens!.accessToken}`
    }
  });

  expect(response.ok(), `Failed to load kurin members: ${response.status()} ${await response.text()}`).toBe(true);
  return await response.json() as MemberResponse[];
}

export async function getMembersByGroup(
  request: APIRequestContext,
  user: E2eUser,
  groupKey: string
): Promise<MemberResponse[]> {
  const login = await loginViaApi(request, user);
  expect(login.tokens?.accessToken, `API login for ${user.email} did not return an access token.`).toBeTruthy();

  const response = await request.get(`${e2eApiUrl}/member/groups/${groupKey}/members`, {
    headers: {
      Authorization: `Bearer ${login.tokens!.accessToken}`
    }
  });

  expect(response.ok(), `Failed to load group members: ${response.status()} ${await response.text()}`).toBe(true);
  return await response.json() as MemberResponse[];
}

export async function assignMentorViaApi(
  request: APIRequestContext,
  user: E2eUser,
  groupKey: string,
  mentorUserKey: string
): Promise<void> {
  const login = await loginViaApi(request, user);
  expect(login.tokens?.accessToken, `API login for ${user.email} did not return an access token.`).toBeTruthy();

  const response = await request.post(`${e2eApiUrl}/group/${groupKey}/mentors/${mentorUserKey}`, {
    headers: {
      Authorization: `Bearer ${login.tokens!.accessToken}`
    }
  });

  expect(response.ok(), `Failed to assign mentor ${mentorUserKey}: ${response.status()} ${await response.text()}`).toBe(true);
}

export async function createLeadershipViaApi(
  request: APIRequestContext,
  user: E2eUser,
  data: {
    type: 'Kurin' | 'Group' | 'KV';
    entityKey: string;
    role: string;
    memberKey: string;
    firstName: string;
    lastName: string;
    middleName?: string;
  }
): Promise<void> {
  const login = await loginViaApi(request, user);
  expect(login.tokens?.accessToken, `API login for ${user.email} did not return an access token.`).toBeTruthy();

  const response = await request.post(`${e2eApiUrl}/leadership`, {
    headers: {
      Authorization: `Bearer ${login.tokens!.accessToken}`
    },
    data: {
      type: data.type,
      entityKey: data.entityKey,
      startDate: new Date().toISOString().slice(0, 10),
      leadershipHistories: [
        {
          role: data.role,
          member: {
            memberKey: data.memberKey,
            firstName: data.firstName,
            lastName: data.lastName,
            middleName: data.middleName ?? ''
          },
          endDate: null
        }
      ]
    }
  });

  expect(response.ok(), `Failed to create ${data.type} leadership: ${response.status()} ${await response.text()}`).toBe(true);
}

export async function getSeededGroupKey(request: APIRequestContext, user: E2eUser, groupName: string): Promise<string> {
  const login = await loginViaApi(request, user);
  expect(login.tokens?.accessToken, `API login for ${user.email} did not return an access token.`).toBeTruthy();
  expect(login.kurinKey, `API login for ${user.email} did not return a kurinKey.`).toBeTruthy();

  const response = await request.get(`${e2eApiUrl}/group/groups`, {
    headers: {
      Authorization: `Bearer ${login.tokens!.accessToken}`
    },
    params: {
      kurinKey: login.kurinKey!
    }
  });

  expect(response.ok(), `Failed to load groups: ${response.status()} ${await response.text()}`).toBe(true);
  const groups = await response.json() as GroupResponse[];
  const group = groups.find(item => item.name === groupName);
  expect(group, `Seeded group ${groupName} was not found.`).toBeTruthy();

  return group!.groupKey;
}

export async function getFirstSeededGroupMemberKey(
  request: APIRequestContext,
  user: E2eUser,
  groupName = 'Gurtok 1'
): Promise<string> {
  const login = await loginViaApi(request, user);
  expect(login.tokens?.accessToken, `API login for ${user.email} did not return an access token.`).toBeTruthy();

  const groupKey = await getSeededGroupKey(request, user, groupName);
  const response = await request.get(`${e2eApiUrl}/member/groups/${groupKey}/members`, {
    headers: {
      Authorization: `Bearer ${login.tokens!.accessToken}`
    }
  });

  expect(response.ok(), `Failed to load group members: ${response.status()} ${await response.text()}`).toBe(true);
  const members = await response.json() as MemberResponse[];
  const member = members.find(item => !!item.memberKey);
  expect(member, `Seeded group ${groupName} has no members.`).toBeTruthy();

  return member!.memberKey;
}

export async function getPlanningSessions(
  request: APIRequestContext,
  user: E2eUser,
  kurinKey?: string
): Promise<PlanningSessionResponse[]> {
  const login = await loginViaApi(request, user);
  expect(login.tokens?.accessToken, `API login for ${user.email} did not return an access token.`).toBeTruthy();
  const resolvedKurinKey = kurinKey ?? login.kurinKey;
  expect(resolvedKurinKey, `API login for ${user.email} did not return a kurinKey.`).toBeTruthy();

  const response = await request.get(`${e2eApiUrl}/planning/${resolvedKurinKey}`, {
    headers: {
      Authorization: `Bearer ${login.tokens!.accessToken}`
    }
  });

  expect(response.ok(), `Failed to load planning sessions: ${response.status()} ${await response.text()}`).toBe(true);
  return await response.json() as PlanningSessionResponse[];
}
