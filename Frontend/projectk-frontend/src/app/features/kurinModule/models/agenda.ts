/** Mirrors the backend AgendaItemKind / AgendaItemStatus / AgendaTargetType enums (serialized as strings). */
export type AgendaItemKind = 'Event' | 'Task';
export type AgendaItemStatus = 'Todo' | 'InProgress' | 'Done';
export type AgendaTargetType = 'Kurin' | 'Group' | 'Member' | 'Leadership';
export type AgendaRsvpStatus = 'Going' | 'NotGoing' | 'Maybe';
export type RecurrenceFrequency = 'None' | 'Weekly' | 'Monthly' | 'Yearly';

/** Weekday bitmask helpers for RecurrenceByWeekday (bit 0 = Sunday … bit 6 = Saturday). */
export const WEEKDAY_BITS = [
  { label: 'Нд', bit: 1 << 0 },
  { label: 'Пн', bit: 1 << 1 },
  { label: 'Вт', bit: 1 << 2 },
  { label: 'Ср', bit: 1 << 3 },
  { label: 'Чт', bit: 1 << 4 },
  { label: 'Пт', bit: 1 << 5 },
  { label: 'Сб', bit: 1 << 6 }
];

export interface AgendaAssignmentDto {
  targetType: AgendaTargetType;
  targetKey: string;
  label: string | null;
}

export interface AgendaItemDto {
  agendaItemKey: string;
  kurinKey: string;
  kind: AgendaItemKind;
  title: string;
  description: string | null;
  status: AgendaItemStatus;
  startUtc: string | null;
  endUtc: string | null;
  isAllDay: boolean;
  createdByUserKey: string;
  createdByName: string | null;
  canEdit: boolean;
  canChangeStatus: boolean;
  categoryKey: string | null;
  categoryName: string | null;
  categoryColorHex: string | null;
  categoryIcon: string | null;
  recurrenceFrequency: RecurrenceFrequency;
  recurrenceInterval: number;
  recurrenceByWeekday: number;
  recurrenceEndUtc: string | null;
  recurrenceCount: number | null;
  isRecurrenceInstance: boolean;
  seriesStartUtc: string | null;
  seriesEndUtc: string | null;
  assignments: AgendaAssignmentDto[];
}

/** An event group (табір/захід/сходини) as the picker and management page see it. */
export interface AgendaCategoryDto {
  agendaCategoryKey: string;
  kurinKey: string;
  name: string;
  colorHex: string;
  icon: string | null;
  capacity: number | null;
  waitlistEnabled: boolean;
  defaultDescription: string | null;
  rsvpRequired: boolean;
  defaultDurationMinutes: number | null;
  reminderLeadMinutes: number | null;
  isArchived: boolean;
}

/** Body for creating/updating an event group. Omit agendaCategoryKey to create. */
export interface UpsertAgendaCategoryRequest {
  agendaCategoryKey?: string | null;
  kurinKey: string;
  name: string;
  colorHex: string;
  icon: string | null;
  capacity: number | null;
  waitlistEnabled: boolean;
  defaultDescription: string | null;
  rsvpRequired: boolean;
  defaultDurationMinutes: number | null;
  reminderLeadMinutes: number | null;
  isArchived: boolean;
}

export interface AgendaRsvpDto {
  userKey: string;
  displayName: string;
  status: AgendaRsvpStatus;
  respondedAtUtc: string;
  isWaitlisted: boolean;
}

export interface AgendaResponsesResponse {
  agendaItemKey: string;
  capacity: number | null;
  waitlistEnabled: boolean;
  myStatus: AgendaRsvpStatus | null;
  goingConfirmedCount: number;
  goingWaitlistCount: number;
  notGoingCount: number;
  maybeCount: number;
  responses: AgendaRsvpDto[];
}

export interface AgendaTargetInput {
  targetType: AgendaTargetType;
  targetKey: string;
}

export interface CreateAgendaItemRequest {
  kurinKey: string;
  kind: AgendaItemKind;
  title: string;
  description: string | null;
  startUtc: string | null;
  endUtc: string | null;
  isAllDay: boolean;
  agendaCategoryKey: string | null;
  recurrenceFrequency: RecurrenceFrequency;
  recurrenceInterval: number;
  recurrenceByWeekday: number;
  recurrenceEndUtc: string | null;
  recurrenceCount: number | null;
  targets: AgendaTargetInput[];
}

export interface UpdateAgendaItemRequest extends CreateAgendaItemRequest {
  agendaItemKey: string;
}

export interface AgendaMemberTarget {
  memberKey: string;
  fullName: string;
}

export interface AgendaLeadershipTarget {
  leadershipKey: string;
  label: string;
  canTarget: boolean;
}

export interface AgendaGroupTarget {
  groupKey: string;
  name: string;
  canTargetGroup: boolean;
  leadership: AgendaLeadershipTarget | null;
  members: AgendaMemberTarget[];
}

export interface AgendaAssignTargets {
  canTargetKurin: boolean;
  kurinKey: string;
  kurinLabel: string;
  kurinLeaderships: AgendaLeadershipTarget[];
  groups: AgendaGroupTarget[];
}
