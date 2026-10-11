/**
 * Camp planning's shapes, kept in step with the web's kurinModule/models/planning-session.dto.ts and
 * the API's PlanningResponse.cs / CreatePlanningSessionCommand.cs. Dates travel as UTC instants.
 */
export interface DateRangeDto {
  start: string;
  end: string;
}

export interface PlanningParticipantDto {
  memberKey: string;
  fullName: string;
  roleWeight: number;
  busyRanges: DateRangeDto[];
}

export interface PlanningSessionDto {
  planningSessionKey: string;
  name: string;
  kurinKey: string;
  searchStart: string;
  searchEnd: string;
  durationDays: number;
  isCalculated: boolean;
  optimalStartDate: string | null;
  optimalEndDate: string | null;
  conflictScore: number;
  /** Its author, or whole-kurin management: the rule the delete endpoint enforces. */
  canDelete: boolean;
  participants: PlanningParticipantDto[];
}

/** The body of `POST planning` (CreatePlanningSessionCommand). */
export interface CreatePlanningRequest {
  name: string;
  kurinKey: string;
  durationDays: number;
  searchStart: string;
  searchEnd: string;
  participants: PlanningParticipantDto[];
}

/** How much one person's clash costs a date (the web's RoleWeight enum). */
export const ROLE_WEIGHTS: readonly { value: number; label: string }[] = [
  { value: 0, label: 'Неважливий' },
  { value: 1, label: 'Низький' },
  { value: 3, label: 'Середній' },
  { value: 5, label: 'Високий' },
];

export const DEFAULT_ROLE_WEIGHT = 3;
