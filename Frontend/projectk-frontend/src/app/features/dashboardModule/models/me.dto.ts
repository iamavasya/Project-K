import { ScoreAlgorithm, ScoreSource } from '../../scoreModule/models/score.enums';
import { AgendaItemStatus, AgendaRsvpStatus } from '../../kurinModule/models/agenda';

/** The kurin a row belongs to; named on screen only when the person stands in more than one. */
export interface MyKurinRefDto {
  kurinKey: string;
  kurinNumber: number;
  namedAfter: string | null;
  isCurrent: boolean;
}

export interface MyEventDto {
  agendaItemKey: string;
  kurin: MyKurinRefDto;
  title: string;
  startUtc: string;
  endUtc: string | null;
  isAllDay: boolean;
  isRecurring: boolean;
  categoryName: string | null;
  categoryColorHex: string | null;
  categoryIcon: string | null;
  rsvpRequired: boolean;
  myResponse: AgendaRsvpStatus | null;
}

export interface MyTaskDto {
  agendaItemKey: string;
  kurin: MyKurinRefDto;
  title: string;
  status: AgendaItemStatus;
  startUtc: string | null;
  endUtc: string | null;
  addressedToMe: boolean;
  canChangeStatus: boolean;
}

// --- Personal growth: проба and вмілості ---

export type MyProbeStatus = 'NotStarted' | 'InProgress' | 'Completed' | 'Verified';
export type MyBadgeStatus = 'Draft' | 'Submitted' | 'Confirmed' | 'Rejected';

export interface MyProbePointDto {
  pointId: string;
  sectionCode: string;
  title: string;
}

export interface MyProbeDto {
  probeId: string;
  title: string;
  status: MyProbeStatus;
  signedPoints: number;
  totalPoints: number;
  /** The next few points still to be signed, in the order of the проба. */
  nextPoints: MyProbePointDto[];
}

export interface MyBadgeDto {
  badgeId: string;
  title: string;
  imagePath: string | null;
  status: MyBadgeStatus;
  submittedAtUtc: string | null;
  reviewedAtUtc: string | null;
}

export interface MyBadgesDto {
  onReview: MyBadgeDto[];
  inWork: MyBadgeDto[];
  confirmed: MyBadgeDto[];
  confirmedCount: number;
}

/** Empty, with the flag off, for anyone whose own branch is not УПЮ. */
export interface MyGrowthDto {
  memberKey: string;
  hasYouthProgram: boolean;
  /** The проба in hand or the next to start; null once every проба is verified. */
  probe: MyProbeDto | null;
  badges: MyBadgesDto;
}

// --- Вкладка ---

export interface MyDuesDto {
  kurin: MyKurinRefDto;
  groupName: string | null;
  quarterYear: number;
  quarterNumber: number;
  /** Negative is debt, positive a surplus. */
  balance: number;
  quarterRate: number | null;
  isConcession: boolean;
}

// --- Точкування ---

export interface MyScoreDto {
  kurin: MyKurinRefDto;
  groupKey: string;
  groupName: string;
  periodLabel: string;
  total: number;
  bySource: Partial<Record<ScoreSource, number>>;
  groupPlace: number;
  groupCount: number;
  groupScore: number;
  algorithm: ScoreAlgorithm;
}

// --- Справи: the провід's queue ---

export type MyDutyKind = 'BadgesToReview' | 'TransfersToConfirm' | 'EntriesToVerify' | 'EventWithoutAttendance';

export interface MyDutyDto {
  kind: MyDutyKind;
  kurin: MyKurinRefDto;
  count: number;
  groupKey: string | null;
  groupName: string | null;
  agendaItemKey: string | null;
  occurrenceStartUtc: string | null;
  title: string | null;
}
