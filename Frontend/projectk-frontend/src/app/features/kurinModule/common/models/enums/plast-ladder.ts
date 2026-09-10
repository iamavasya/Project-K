import { KurinBranch } from './kurin-branch.enum';
import { PlastLevel } from './plast-level.enum';

/**
 * Порядок, у якому людина проходить ступені, і що з них курінь кожної гілки звично записує.
 *
 * Порядок живе тут, а не в самому enum: ступінь зберігається числом, і числа заморожені —
 * `Prykhylnyk` дописаний останнім, хоч на драбині стоїть другим. Дзеркалить `PlastLadder` на бекенді.
 */
export const PLAST_LADDER: readonly PlastLevel[] = [
  PlastLevel.Entry,
  PlastLevel.Prykhylnyk,
  PlastLevel.Uchasnyk,
  PlastLevel.Rozviduvach,
  PlastLevel.Skob,
  PlastLevel.HetmanskiySkob,
  PlastLevel.Starshoplastun,
  PlastLevel.Senior,
  PlastLevel.SeniorPratsi,
  PlastLevel.SeniorDovirja,
  PlastLevel.SeniorKerivnytstva
];

/**
 * Підпис ступеня — так, як його пишуть у звітах. Дата вступу до Пласту стоїть під «пл. неім.»: це і
 * є ступінь пластуна неіменованого, а не окремий службовий рядок перед драбиною.
 */
export const PLAST_LEVEL_COLUMN_LABELS: Record<PlastLevel, string> = {
  [PlastLevel.Entry]: 'пл. неім.',
  [PlastLevel.Prykhylnyk]: 'пл. прих.',
  [PlastLevel.Uchasnyk]: 'пл. уч.',
  [PlastLevel.Rozviduvach]: 'пл. розв.',
  [PlastLevel.Skob]: 'пл. скоб / вірл.',
  [PlastLevel.HetmanskiySkob]: 'пл. гетьм. скоб / вірл.',
  [PlastLevel.Starshoplastun]: 'Старшопластун',
  [PlastLevel.Senior]: 'Перехід в УПС',
  [PlastLevel.SeniorPratsi]: 'Сен. праці',
  [PlastLevel.SeniorDovirja]: "Сен. довір'я",
  [PlastLevel.SeniorKerivnytstva]: 'Сен. керівництва'
};

const THROUGH_YOUTH: readonly PlastLevel[] = PLAST_LADDER.slice(0, 6);
const THROUGH_SENIOR: readonly PlastLevel[] = PLAST_LADDER.slice(0, 7);

/**
 * Які ступені показувати куреню цієї гілки **за замовчуванням**. Накопичувально: УПС-курінь однаково
 * хоче бачити, коли його люди були заприсяжені. Це дефолт, а не обмеження — далі провід вмикає й
 * вимикає колонки сам.
 */
export function defaultLevelsFor(branch: KurinBranch | null | undefined): readonly PlastLevel[] {
  switch (branch) {
    case KurinBranch.USP:
      return THROUGH_SENIOR;
    case KurinBranch.UPS:
      return PLAST_LADDER;
    default:
      return THROUGH_YOUTH;
  }
}
