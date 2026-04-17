import { BadgeProgressStatus } from '../enums/badge-progress-status.enum';

export interface MemberSkillItemView {
  badgeId: string;
  title: string;
  imageUrl: string | null;
  status: BadgeProgressStatus;
  submittedAtUtc: string | null;
  reviewedAtUtc: string | null;
  isPendingConfirmation: boolean;
}