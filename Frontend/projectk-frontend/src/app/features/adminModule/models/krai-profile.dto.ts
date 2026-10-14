/** A край's profile as it is stored. Every value is optional: what is left out comes from code. */
export interface KraiProfileDocument {
  catalog?: {
    /** Which краї's вмілості the kurins see; unset means the край's own. */
    badgeSources?: string[];
    hiddenBadges?: string[];
  };
  locale?: {
    currency?: string;
    timeZone?: string;
    numberLocale?: string;
  };
}

export interface KraiProfileEditorDto {
  kraiCode: string;
  kraiName: string;
  /** The revision in force; 0 when none was ever saved. */
  revision: number;
  document: KraiProfileDocument;
}

export interface KraiProfileRevisionDto {
  revision: number;
  createdAtUtc: string;
  createdByName: string | null;
  note: string | null;
}
