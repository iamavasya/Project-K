/**
 * The shapes the leader screens read, kept in step with the web (notificationsModule/models,
 * kurinModule/models/probes-and-badges, duesModule/models) and the API's DTOs they mirror.
 */

// ── Notifications (api/notifications; AppNotificationDto) ─────────────────────────────────────

export type AppNotificationType =
  | 'MemberProfileVerified'
  | 'MemberProfileChangedAfterVerification'
  | 'MemberSkillSubmittedForReview'
  | 'MemberAwardSubmitted'
  | 'MemberAwardReviewed'
  | 'MemberWarningAssigned'
  | 'LeadershipChanged'
  | 'MemberSkillReviewed'
  | 'AgendaItemAssigned'
  | 'AgendaItemUpdated'
  | 'AgendaItemStatusChanged'
  | 'AgendaItemDeleted'
  | 'WaitlistEntrySubmitted';

export type AppNotificationSeverity = 'Info' | 'Success' | 'Warn' | 'Error';

export interface AppNotification {
  notificationKey: string;
  type: AppNotificationType;
  severity: AppNotificationSeverity;
  title: string;
  body: string;
  entityType: string | null;
  entityKey: string | null;
  /** The web's screen path, e.g. `/member/{key}`; see notification-route.ts for the phone's. */
  route: string | null;
  createdAtUtc: string;
  readAtUtc: string | null;
  isRead: boolean;
}

// ── Skills review (api/kurin/{k}/badges/review, api/catalog/badges) ───────────────────────────

export interface BadgeProgressDto {
  badgeProgressKey: string;
  memberKey: string;
  kurinKey: string;
  badgeId: string;
  status: string;
  submittedAtUtc: string | null;
  memberFirstName?: string | null;
  memberLastName?: string | null;
}

export interface BadgeCatalogItemDto {
  id: string;
  title: string;
  specialization: string;
  level: number | null;
  seekerRequirements: string;
}

export interface ReviewBadgeProgressRequest {
  isApproved: boolean;
  note: string | null;
}

// ── Group dues (api/group/{groupKey}/dues; GroupDuesDto) ──────────────────────────────────────

export type DuesEntryKind =
  | 'Contribution'
  | 'Refund'
  | 'TransferToKurin'
  | 'TransferToStanytsia'
  | 'Expense'
  | 'OtherIncome'
  | 'Exchange'
  | 'Correction';

export type DuesPaymentMethod = 'Cash' | 'Card';

export type DuesAccountStanding = 'Current' | 'Moved' | 'Left';

export interface QuarterDto {
  year: number;
  number: number;
}

export interface PlastYearDto {
  startYear: number;
  label: string;
  quarters: QuarterDto[];
}

export interface KurinDuesRateDto {
  fromQuarter: QuarterDto;
  stanytsiaFull: number;
  stanytsiaReduced: number;
  kurinShare: number;
}

export interface GroupDuesRateDto {
  fromQuarter: QuarterDto;
  groupShare: number;
}

export interface DuesAmountDto {
  stanytsia: number;
  kurin: number;
  group: number;
  total: number;
}

export interface DuesAccountQuarterDto {
  quarter: QuarterDto;
  charged: DuesAmountDto;
  paid: DuesAmountDto;
  balance: number;
  isConcession: boolean;
}

export interface DuesAccountDto {
  membershipKey: string;
  memberKey: string;
  fullName: string;
  standing: DuesAccountStanding;
  isConcessionNow: boolean;
  quarters: DuesAccountQuarterDto[];
  charged: number;
  payments: number;
  balance: number;
}

export interface DuesEntryDto {
  duesEntryKey: string;
  kind: DuesEntryKind;
  method: DuesPaymentMethod;
  counterMethod: DuesPaymentMethod | null;
  amount: number;
  occurredOn: string;
  membershipKey: string | null;
  memberName: string | null;
  collectedByMemberKey: string | null;
  collectedByName: string | null;
  note: string | null;
  isVerified: boolean;
  verifiedAtUtc: string | null;
  verifiedByName: string | null;
  createdAtUtc: string;
}

export interface DuesBoxDto {
  cash: number;
  card: number;
  total: number;
  toForward: number;
  own: number;
  inTransit: number;
}

export interface DuesHandoverDto {
  owedUp: number;
  transferred: number;
  received: number;
  outstanding: number;
}

export interface DuesPersonDto {
  memberKey: string;
  fullName: string;
}

export interface DuesViewerDto {
  canKeep: boolean;
  canVerify: boolean;
  canSetKurinRates: boolean;
}

export interface GroupDuesDto {
  groupKey: string;
  kurinKey: string;
  groupName: string;
  currentQuarter: QuarterDto;
  years: PlastYearDto[];
  kurinRates: KurinDuesRateDto[];
  groupRates: GroupDuesRateDto[];
  accounts: DuesAccountDto[];
  entries: DuesEntryDto[];
  box: DuesBoxDto;
  handover: DuesHandoverDto;
  people: DuesPersonDto[];
  viewer: DuesViewerDto;
}

export interface UpsertDuesEntryRequest {
  kind: DuesEntryKind;
  method: DuesPaymentMethod;
  counterMethod: DuesPaymentMethod | null;
  amount: number;
  occurredOn: string;
  membershipKey: string | null;
  collectedByMemberKey: string | null;
  note: string | null;
}

export interface SetGroupDuesRateRequest {
  fromQuarter: QuarterDto;
  groupShare: number;
}

export interface SetDuesConcessionRequest {
  fromQuarter: QuarterDto;
  isConcession: boolean;
}
