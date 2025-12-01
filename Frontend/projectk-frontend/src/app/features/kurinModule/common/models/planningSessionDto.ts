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

  participants: PlanningParticipantDto[];
}