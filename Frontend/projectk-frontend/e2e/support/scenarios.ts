import { APIRequestContext, TestInfo } from '@playwright/test';

import {
  activateAccount,
  assignMentorViaApi,
  createGroupViaApi,
  createMemberViaApi,
  CreatedGroup,
  CreatedMember,
  getLatestInvitationByEmail,
  getSeededKurinKey
} from './api-client';
import { E2eUser } from './test-users';

export interface ActivatedMemberAccount {
  member: CreatedMember;
  user: E2eUser;
}

export interface GroupWithMentorScenario {
  kurinKey: string;
  group: CreatedGroup;
  mentorAccount: ActivatedMemberAccount;
}

export function scenarioSuffix(testInfo: TestInfo): string {
  return `${testInfo.project.name.replace(/\W/g, '')}-${Date.now().toString().slice(-8)}`;
}

export function scenarioEmail(suffix: string, label: string): string {
  return `e2e.${label}.${suffix}@example.com`;
}

export function memberFullName(member: Pick<CreatedMember, 'firstName' | 'lastName'>): string {
  return `${member.lastName} ${member.firstName}`;
}

export async function createActivatedMemberAccount(
  request: APIRequestContext,
  owner: E2eUser,
  scope: { groupKey: string } | { kurinKey: string },
  member: {
    firstName: string;
    middleName: string;
    lastName: string;
    email: string;
  },
  password: string,
  role: E2eUser['role']
): Promise<ActivatedMemberAccount> {
  const created = await createMemberViaApi(request, owner, scope, {
    ...member,
    createUserAccount: true
  });
  const invitation = await getLatestInvitationByEmail(request, member.email);
  await activateAccount(request, invitation.token, password);

  return {
    member: created,
    user: {
      role,
      email: member.email,
      password
    }
  };
}

export async function createGroupWithMentorScenario(
  request: APIRequestContext,
  manager: E2eUser,
  suffix: string,
  password: string
): Promise<GroupWithMentorScenario> {
  const kurinKey = await getSeededKurinKey(request, manager);
  const group = await createGroupViaApi(request, manager, kurinKey, `E2E Scenario Group ${suffix}`);
  const mentorAccount = await createActivatedMemberAccount(
    request,
    manager,
    { kurinKey },
    {
      firstName: `ScenarioMentor${suffix}`,
      middleName: 'Account',
      lastName: 'Candidate',
      email: scenarioEmail(suffix, 'scenario.mentor')
    },
    password,
    'mentor'
  );

  await assignMentorViaApi(request, manager, group.groupKey, mentorAccount.member.userKey!);

  return {
    kurinKey,
    group,
    mentorAccount
  };
}
