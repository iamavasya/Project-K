import { AgendaItemStatus } from './agenda';

/** Optimus UI p-tag severities available on the Лілейка theme (no new colours introduced). */
export type TagSeverity = 'secondary' | 'success' | 'info' | 'warn' | 'danger' | 'contrast';

export interface AgendaStatusMeta {
  label: string;
  /**
   * Тег-колір статусу. BRANDBOOK §2 виняток: на дошці задач `warn` (теракота) використовується для
   * «В процесі» попри правило «одна теракота на екран» — це єдиний теплий тон у палітрі.
   */
  severity: TagSeverity;
  /** Ліва→права позиція колонки на дошці. */
  order: number;
}

/**
 * Єдине джерело для колонок дошки та кольорів статусів. Додати статус = один запис тут
 * (+ значення в бекенд-enum AgendaItemStatus). Кольори лише з наявних `severity`.
 */
export const AGENDA_STATUS_META: Record<AgendaItemStatus, AgendaStatusMeta> = {
  Todo: { label: 'Зробити', severity: 'secondary', order: 0 },
  InProgress: { label: 'В процесі', severity: 'warn', order: 1 },
  Done: { label: 'Зроблено', severity: 'info', order: 2 }
};

/** Колонки дошки в порядку зліва направо. */
export const AGENDA_BOARD_COLUMNS: AgendaItemStatus[] = (Object.keys(AGENDA_STATUS_META) as AgendaItemStatus[])
  .sort((a, b) => AGENDA_STATUS_META[a].order - AGENDA_STATUS_META[b].order);

/**
 * Гранулярність календаря. `'day'` — подобовий рендер (типово). Перемкнути на `'hour'`, коли зʼявиться
 * вимога погодинного відображення; БД уже зберігає повний час, тож міграція не потрібна — лише інший
 * шаблон сітки.
 */
export const AGENDA_CALENDAR_MODE: 'day' | 'hour' = 'day';
