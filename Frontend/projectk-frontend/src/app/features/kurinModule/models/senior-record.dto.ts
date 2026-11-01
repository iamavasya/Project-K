/** What a line of a senior's книжечка is. */
export type SeniorRecordKind = 'Degree' | 'Award' | 'Training';

/** A ступінь, відзначення or вишкіл from the catalogue (`GET api/catalog/senior`). */
export interface SeniorCatalogItem {
  id: string;
  kind: SeniorRecordKind;
  title: string;
  /** A ступінь's улад, or an award's ladder: `lsh`, `orden-sv-yuriia`… */
  group: string;
  ulad: string;
  order: number;
  imageUrl: string;
}

/** One line of the person's own книжечка; named from the catalogue by `catalogId`. */
export interface MemberSeniorRecordDto {
  memberSeniorRecordKey: string;
  catalogId: string;
  number?: string | null;
  /** `yyyy-MM-dd`. */
  achievedOn?: string | null;
  note?: string | null;
}

export interface UpsertSeniorRecordRequest {
  catalogId: string;
  number?: string | null;
  achievedOn?: string | null;
  note?: string | null;
}
