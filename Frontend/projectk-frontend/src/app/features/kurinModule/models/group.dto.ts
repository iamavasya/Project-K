export interface GroupDto {
  groupKey: string;
  kurinKey: string;
  name: string;
  description?: string | null;
  silhouetteUrl?: string | null;
  /** Null for a підготовчий курінь. */
  kurinNumber: number | null;
}
