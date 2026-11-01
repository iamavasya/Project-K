import { MemberSeniorRecordDto, SeniorCatalogItem, SeniorRecordKind } from '../../../../models/senior-record.dto';
import { parseDateOnlyString } from '../../../../functions/to-date-only-string.function';

/** A line of the книжечка with what the catalogue says of it. */
export interface BookRow {
  record: MemberSeniorRecordDto;
  title: string;
  imageUrl: string;
}

export interface BookSection {
  kind: SeniorRecordKind;
  title: string;
  rows: BookRow[];
}

export const BOOK_KINDS: readonly SeniorRecordKind[] = ['Degree', 'Award', 'Training'];

export const BOOK_SECTION_TITLES: Record<SeniorRecordKind, string> = {
  Degree: 'Ступені',
  Award: 'Відзначення',
  Training: 'Вишколи'
};

/**
 * Ступені, відзначення, вишколи — кожне окремо, новіше вгорі. Рядок, якого каталог уже не знає,
 * лишається під своїм id серед відзначень: людина його вносила, і він не мусить зникнути.
 */
export function bookSections(records: readonly MemberSeniorRecordDto[], catalog: readonly SeniorCatalogItem[]): BookSection[] {
  const byId = new Map(catalog.map(item => [item.id, item]));
  const rows = [...records]
    .sort((a, b) => (b.achievedOn ?? '').localeCompare(a.achievedOn ?? ''))
    .map(record => {
      const item = byId.get(record.catalogId);
      const kind: SeniorRecordKind = item?.kind ?? 'Award';
      return { kind, row: { record, title: item?.title ?? record.catalogId, imageUrl: item?.imageUrl ?? '' } };
    });

  return BOOK_KINDS
    .map(kind => ({ kind, title: BOOK_SECTION_TITLES[kind], rows: rows.filter(r => r.kind === kind).map(r => r.row) }))
    .filter(section => section.rows.length > 0);
}

/** «ЛШ 41 · 20.08.1999»: the number and the date, whichever are there. */
export function recordLine(record: MemberSeniorRecordDto): string {
  const date = parseDateOnlyString(record.achievedOn ?? null);
  const day = date
    ? `${String(date.getDate()).padStart(2, '0')}.${String(date.getMonth() + 1).padStart(2, '0')}.${date.getFullYear()}`
    : null;
  return [record.number, day].filter(Boolean).join(' · ');
}
