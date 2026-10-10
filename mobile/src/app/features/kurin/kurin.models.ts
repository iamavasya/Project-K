/**
 * The kurin, its groups and member cards as the API sends them. Kept in step with the web's
 * kurinModule/models (kurin.dto, group.dto, member.dto, membership.dto, member-award.dto,
 * requests/member/member-lookup.dto, requests/leadership/leadership.dto, probes-and-badges/*) and
 * duesModule/models/group-dues.dto (MemberDuesDto). Enums travel as strings (JsonStringEnumConverter).
 */

export type KurinBranch = 'UPYu' | 'USP' | 'UPS';

export interface KurinDto {
  kurinKey: string;
  number: number;
  branch?: KurinBranch;
  stanytsia?: string | null;
  regionOrCountry?: string | null;
  namedAfter?: string | null;
  description?: string | null;
  profileVerificationEnabled?: boolean;
}

export interface GroupDto {
  groupKey: string;
  kurinKey: string;
  name: string;
  description?: string | null;
  silhouetteUrl?: string | null;
  kurinNumber: number;
}

export type ProfileVerificationStatus = 'Unverified' | 'VerifiedStale' | 'VerifiedCurrent';
export type WarningLevel = 'Level1' | 'Level2' | 'Level3';

export interface MemberWarningDto {
  memberWarningKey: string;
  level: WarningLevel;
  issuedAtUtc: string;
  expiresAtUtc: string;
  revokedAtUtc?: string | null;
}

export interface LeadershipHistoryDto {
  leadershipHistoryKey: string;
  leadershipKey: string;
  role: string;
  leadershipType?: string | null;
  groupName?: string | null;
  startDate: string;
  endDate: string | null;
  member: MemberLookupDto;
}

export interface LeadershipDto {
  leadershipKey: string | null;
  startDate: string;
  endDate: string | null;
  leadershipHistories: LeadershipHistoryDto[];
}

/** A row of a member list (`member/kurins/{k}/members`, `member/groups/{g}/members`, `members/kv/{k}`). */
export interface MemberLookupDto {
  memberKey: string;
  userKey?: string | null;
  userRole?: string | null;
  firstName: string;
  middleName?: string | null;
  lastName: string;
  profilePhotoUrl?: string | null;
  latestPlastLevel?: string | null;
  phoneNumber?: string | null;
  dateOfBirth?: string | null;
  profileVerificationStatus?: ProfileVerificationStatus | null;
  leadershipHistories?: LeadershipHistoryDto[];
  mentoredGroupNames?: string[];
  warnings?: MemberWarningDto[];
  plastLevelHistories?: PlastLevelHistoryDto[];
}

export interface PlastLevelHistoryDto {
  memberKey?: string;
  plastLevelHistoryKey?: string;
  plastLevel: string;
  dateAchieved?: string | null;
}

export type AwardLevel = 'First' | 'Second' | 'Third' | 'Fourth';
export type ProgressStatus = 'Draft' | 'Submitted' | 'Confirmed' | 'Rejected';

export interface MemberAwardDto {
  memberAwardKey: string;
  memberKey: string;
  kurinKey: string;
  level: AwardLevel;
  dateAcquired: string;
  note?: string | null;
  status: ProgressStatus | number;
  imageUrl?: string | null;
}

/** `GET member/{memberKey}`: the card. */
export interface MemberDto extends MemberLookupDto {
  publicId?: string;
  groupKey?: string | null;
  groupName?: string | null;
  kurinKey?: string | null;
  email: string;
  address?: string | null;
  school?: string | null;
  mentoredGroups?: { groupKey: string; name: string }[];
  awards?: MemberAwardDto[];
  profileVerifiedAtUtc?: string | null;
}

export interface MembershipDto {
  membershipKey: string;
  kurinKey: string;
  kurinNumber: number;
  branch: KurinBranch;
  kurinNamedAfter?: string | null;
  groupKey?: string | null;
  groupName?: string | null;
  kind: 'Youth' | 'Staff';
  joinedAtUtc: string;
  leftAtUtc?: string | null;
  isCurrent: boolean;
}

export interface MentorAssignmentDto {
  mentorAssignmentKey: string;
  mentorUserKey: string;
  groupKey: string;
  groupName: string;
  assignedAtUtc: string;
  revokedAtUtc: string | null;
  member: MemberLookupDto | null;
}

// ── Skills and probes (ProbesAndBadgesModule) ────────────────────────────────────────────────────

export interface BadgeCatalogItemDto {
  id: string;
  title: string;
  imagePath: string;
  country: string;
  specialization: string;
}

export interface BadgeProgressDto {
  badgeProgressKey: string;
  memberKey: string;
  badgeId: string;
  status: ProgressStatus | number;
  submittedAtUtc: string | null;
  reviewedAtUtc: string | null;
}

export type ProbeStatus = 'NotStarted' | 'InProgress' | 'Completed' | 'Verified';

export interface ProbeSummaryDto {
  id: string;
  title: string;
  pointsCount: number;
  sectionsCount: number;
}

export interface GroupedProbeDto {
  id: string;
  title: string;
  pointsCount: number;
  sectionsCount: number;
  sections: { id: string; code: string; title: string; points: { id: string; title: string }[] }[];
}

export interface ProbePointProgressDto {
  pointId: string;
  isSigned: boolean;
  signedAtUtc: string | null;
  signedByUserKey: string | null;
  signedByName: string | null;
  signedByRole: string | null;
}

export interface ProbeProgressDto {
  probeId: string;
  status: ProbeStatus | number;
  completedAtUtc: string | null;
  completedByUserKey?: string | null;
  completedByName?: string | null;
  completedByRole?: string | null;
  verifiedAtUtc: string | null;
  verifiedByUserKey?: string | null;
  verifiedByName?: string | null;
  verifiedByRole?: string | null;
  pointSignatures?: ProbePointProgressDto[];
}

// ── Dues (duesModule/models/group-dues.dto, MemberDuesDto) ───────────────────────────────────────

export interface QuarterDto {
  year: number;
  number: number;
}

export interface DuesEntryDto {
  duesEntryKey: string;
  kind: string;
  amount: number;
  occurredOn: string;
}

export interface MemberDuesDto {
  hasAccount: boolean;
  kurinKey: string;
  currentQuarter: QuarterDto;
  balance: number;
  quarterRate: { total: number } | null;
  isConcessionNow: boolean;
  currentGroupKey: string | null;
  currentGroupName: string | null;
  canOpenGroupDues: boolean;
  accounts: { groupKey: string; groupName: string; standing: 'Current' | 'Moved' | 'Left'; balance: number }[];
  entries: DuesEntryDto[];
}

// ── Writes ───────────────────────────────────────────────────────────────────────────────────────

export interface UpsertAwardRequest {
  memberAwardKey?: string;
  level: AwardLevel;
  dateAcquired: string;
  note?: string;
}

/** The fields of one's own card the phone edits (web upsert-member, youth: no email change). */
export interface OwnProfileForm {
  firstName: string;
  middleName: string;
  lastName: string;
  phoneNumber: string;
  dateOfBirth: string;
}
