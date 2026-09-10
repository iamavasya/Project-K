import { MemberDto } from '../common/models/memberDto';
import { KurinBranch } from '../common/models/enums/kurin-branch.enum';
import { PlastLevel } from '../common/models/enums/plast-level.enum';
import { PLAST_LADDER, PLAST_LEVEL_COLUMN_LABELS, defaultLevelsFor } from '../common/models/enums/plast-ladder';

/** Як малюється клітинка. Дати йдуть через `date`, решта — текстом. */
export type RegistryColumnKind = 'text' | 'date';

export interface RegistryColumn {
  readonly id: string;
  readonly header: string;
  /** Група в перемикачі колонок, щоб одинадцять дат не лежали одним списком. */
  readonly group: 'Особа' | 'Контакти' | 'Належність' | 'Ступені';
  readonly kind: RegistryColumnKind;
  /** Значення для сортування, фільтра й друку. */
  readonly value: (member: MemberDto) => string | Date | null;
}

function levelDate(member: MemberDto, level: PlastLevel): Date | null {
  const entry = (member.plastLevelHistories ?? []).find(history => history.plastLevel === level);
  if (!entry?.dateAchieved) {
    return null;
  }

  return entry.dateAchieved instanceof Date ? entry.dateAchieved : new Date(entry.dateAchieved);
}

const LEVEL_COLUMNS: RegistryColumn[] = PLAST_LADDER.map(level => ({
  id: `level:${level}`,
  header: PLAST_LEVEL_COLUMN_LABELS[level],
  group: 'Ступені' as const,
  kind: 'date' as const,
  value: (member: MemberDto) => levelDate(member, level)
}));

/**
 * Кожна колонка реєстру. Прізвище й ім'я не тут — вони в замороженій першій колонці, яку не
 * вимкнути: рядок без імені нічого не означає.
 */
export const REGISTRY_COLUMNS: RegistryColumn[] = [
  {
    id: 'dateOfBirth',
    header: 'Дата народження',
    group: 'Особа',
    kind: 'date',
    value: member => member.dateOfBirth ?? null
  },
  {
    id: 'plastLevel',
    header: 'Пластовий ступінь',
    group: 'Особа',
    kind: 'text',
    value: member => member.latestPlastLevelDisplay ?? null
  },
  {
    id: 'groupName',
    header: 'Гурток',
    group: 'Належність',
    kind: 'text',
    value: member => member.groupName ?? null
  },
  {
    id: 'phoneNumber',
    header: 'Телефон',
    group: 'Контакти',
    kind: 'text',
    value: member => member.phoneNumber || null
  },
  {
    id: 'email',
    header: 'Пошта',
    group: 'Контакти',
    kind: 'text',
    value: member => member.email || null
  },
  {
    id: 'address',
    header: 'Адреса',
    group: 'Контакти',
    kind: 'text',
    value: member => member.address ?? null
  },
  {
    id: 'school',
    header: 'Школа',
    group: 'Контакти',
    kind: 'text',
    value: member => member.school ?? null
  },
  ...LEVEL_COLUMNS
];

/**
 * Що курінь цієї гілки бачить, поки нічого не вибирав. Особа й належність — завжди; з дат — те, що
 * гілка звично записує.
 */
export function defaultColumnIdsFor(branch: KurinBranch | null | undefined): string[] {
  return [
    'dateOfBirth',
    'plastLevel',
    'groupName',
    'phoneNumber',
    ...defaultLevelsFor(branch).map(level => `level:${level}`)
  ];
}
