/** Mirrors `ScoreSource` on the backend; enums travel as strings. */
export enum ScoreSource {
  Attendance = 'Attendance',
  Item = 'Item',
  Free = 'Free',
  Skill = 'Skill',
  ProbePoint = 'ProbePoint',
  Probe = 'Probe',
  Dues = 'Dues',
  Warning = 'Warning'
}

export enum ScoreAlgorithm {
  Average = 'Average',
  Sum = 'Sum'
}

export const SCORE_SOURCE_LABELS: Record<ScoreSource, string> = {
  [ScoreSource.Attendance]: 'Присутність',
  [ScoreSource.Item]: 'Позиції',
  [ScoreSource.Free]: 'Від судді',
  [ScoreSource.Skill]: 'Вмілості',
  [ScoreSource.ProbePoint]: 'Точки проби',
  [ScoreSource.Probe]: 'Проби',
  [ScoreSource.Dues]: 'Вкладка',
  [ScoreSource.Warning]: 'Перестороги'
};

/** The order the breakdown columns go in: what is given by hand first, then what the system knows. */
export const SCORE_SOURCE_ORDER: readonly ScoreSource[] = [
  ScoreSource.Attendance,
  ScoreSource.Item,
  ScoreSource.Free,
  ScoreSource.Skill,
  ScoreSource.ProbePoint,
  ScoreSource.Probe,
  ScoreSource.Dues,
  ScoreSource.Warning
];

/** The sources a суддя куреня prices by rule; attendance is priced per group of events. */
export const AUTOMATIC_SCORE_SOURCES: readonly ScoreSource[] = [
  ScoreSource.Skill,
  ScoreSource.ProbePoint,
  ScoreSource.Probe,
  ScoreSource.Dues,
  ScoreSource.Warning
];

export const SCORE_ALGORITHM_LABELS: Record<ScoreAlgorithm, string> = {
  [ScoreAlgorithm.Average]: 'Середнє на юнака',
  [ScoreAlgorithm.Sum]: 'Сума балів'
};

export const SCORE_ALGORITHM_HINTS: Record<ScoreAlgorithm, string> = {
  [ScoreAlgorithm.Average]: 'Бали юнаків ділимо на кількість юнаків у гуртку. Малий активний гурток не програє великому пасивному.',
  [ScoreAlgorithm.Sum]: 'Усі бали юнаків складаємо. Великий гурток має перевагу.'
};
