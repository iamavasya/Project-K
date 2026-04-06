import { MemberDto } from '../models/memberDto';
import { PlastLevel } from '../models/enums/plast-level.enum';
import { PlastLevelHistoryDto } from '../models/plastLevelHistoryDto';

const PLAST_LEVEL_DISPLAY_MAP: Record<PlastLevel, string> = {
  [PlastLevel.Entry]: 'пл. прих.',
  [PlastLevel.Uchasnyk]: 'пл. уч.',
  [PlastLevel.Rozviduvach]: 'пл. розв.',
  [PlastLevel.Skob]: 'пл. скоб',
  [PlastLevel.HetmanskiySkob]: 'пл. гетьм. скоб',
  [PlastLevel.Starshoplastun]: 'ст. пл.',
  [PlastLevel.Senior]: 'пл. сен.',
  [PlastLevel.SeniorPratsi]: 'пл. сен. пр.',
  [PlastLevel.SeniorDovirja]: 'пл. сен. дов.',
  [PlastLevel.SeniorKerivnytstva]: 'пл. сен. кер.'
};

const STARSHOPLASTUN_SUFFIX_BY_LEVEL: Record<PlastLevel.Skob | PlastLevel.HetmanskiySkob, string> = {
  [PlastLevel.Skob]: 'скоб',
  [PlastLevel.HetmanskiySkob]: 'гетьм. скоб'
};

function toPlastLevel(value: string | null | undefined): PlastLevel | null {
  if (!value) {
    return null;
  }

  return (Object.values(PlastLevel) as string[]).includes(value) ? (value as PlastLevel) : null;
}

function resolveStarshoplastunSuffix(histories: PlastLevelHistoryDto[] | null | undefined): string | null {
  if (!histories?.length) {
    return null;
  }

  const hasHetmanskiySkob = histories.some(history => history.plastLevel === PlastLevel.HetmanskiySkob);
  if (hasHetmanskiySkob) {
    return STARSHOPLASTUN_SUFFIX_BY_LEVEL[PlastLevel.HetmanskiySkob];
  }

  const hasSkob = histories.some(history => history.plastLevel === PlastLevel.Skob);
  if (hasSkob) {
    return STARSHOPLASTUN_SUFFIX_BY_LEVEL[PlastLevel.Skob];
  }

  return null;
}

export function localizeLatestPlastLevel(member: Pick<MemberDto, 'latestPlastLevel' | 'plastLevelHistories'>): string | null {
  const latestPlastLevel = toPlastLevel(member.latestPlastLevel);
  if (!latestPlastLevel) {
    return member.latestPlastLevel ?? null;
  }

  if (latestPlastLevel === PlastLevel.Starshoplastun) {
    const suffix = resolveStarshoplastunSuffix(member.plastLevelHistories);
    if (suffix) {
      return `${PLAST_LEVEL_DISPLAY_MAP[latestPlastLevel]} ${suffix}`;
    }
  }

  return PLAST_LEVEL_DISPLAY_MAP[latestPlastLevel];
}

export function mapMemberForView(member: MemberDto): MemberDto {
  const latestPlastLevelDisplay = localizeLatestPlastLevel(member);

  if (!latestPlastLevelDisplay) {
    return member;
  }

  return {
    ...member,
    latestPlastLevelDisplay
  };
}