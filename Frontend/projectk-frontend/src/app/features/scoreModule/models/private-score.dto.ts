import { ScorePeriodDto, ScorePeriodsDto } from './score.dto';

export interface PrivateScoreCriterionDto {
  privateScoreCriterionKey: string;
  name: string;
  isArchived: boolean;
}

export interface PrivateScoreEntryDto {
  privateScoreEntryKey: string;
  membershipKey: string;
  memberKey: string;
  memberName: string;
  privateScoreCriterionKey: string | null;
  criterionName: string | null;
  points: number;
  note: string | null;
  occurredOn: string;
  createdByName: string | null;
  createdAtUtc: string;
}

export interface PrivateScorePersonDto {
  membershipKey: string;
  memberKey: string;
  fullName: string;
  groupKey: string | null;
  groupName: string;
  publicTotal: number;
  privateTotal: number;
  byCriterion: Record<string, number>;
  uncategorised: number;
}

export interface PrivateScoreDto {
  kurinKey: string;
  period: ScorePeriodDto;
  periods: ScorePeriodsDto;
  criteria: PrivateScoreCriterionDto[];
  people: PrivateScorePersonDto[];
  entries: PrivateScoreEntryDto[];
}

export interface UpsertPrivateScoreEntryRequest {
  membershipKey: string;
  privateScoreCriterionKey: string | null;
  points: number;
  note: string | null;
  occurredOn: string;
}

export interface UpsertPrivateScoreCriterionRequest {
  name: string;
  isArchived: boolean;
}
