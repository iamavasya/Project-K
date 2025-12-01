export interface DateRangeDto {
  start: string; // ISO String ("2025-06-01")
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

  // Параметри
  searchStart: string; // ISO String
  searchEnd: string;
  durationDays: number;

  // Результат
  isCalculated: boolean; // Використовуємо це замість статусу 'New' | 'Calculated'
  optimalStartDate?: string | null;
  optimalEndDate?: string | null;
  conflictScore: number;

  // Для списку цей масив може бути пустим, для деталей - повним
  participants: PlanningParticipantDto[];
}