/** Mirrors the backend AgendaItemKind / AgendaItemStatus / AgendaTargetType enums (serialized as strings). */
export type AgendaItemKind = 'Event' | 'Task';
export type AgendaItemStatus = 'Todo' | 'InProgress' | 'Done';
export type AgendaTargetType = 'Kurin' | 'Group' | 'Member';

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
  canEdit: boolean;
  canChangeStatus: boolean;
  assignments: AgendaAssignmentDto[];
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
  targets: AgendaTargetInput[];
}

export interface UpdateAgendaItemRequest extends CreateAgendaItemRequest {
  agendaItemKey: string;
}

export interface AgendaMemberTarget {
  memberKey: string;
  fullName: string;
}

export interface AgendaGroupTarget {
  groupKey: string;
  name: string;
  canTargetGroup: boolean;
  members: AgendaMemberTarget[];
}

export interface AgendaAssignTargets {
  canTargetKurin: boolean;
  kurinKey: string;
  kurinLabel: string;
  groups: AgendaGroupTarget[];
}
