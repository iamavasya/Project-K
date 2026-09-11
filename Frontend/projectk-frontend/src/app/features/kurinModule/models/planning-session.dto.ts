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
  optimalStartDate?: string | null;
  optimalEndDate?: string | null;
  conflictScore: number;

  /**
   * Whether this viewer may withdraw the session. Decided by the backend — its author, or whole-kurin
   * management — so the button is offered on the same rule the endpoint enforces.
   */
  canDelete: boolean;

  participants: PlanningParticipantDto[];
}