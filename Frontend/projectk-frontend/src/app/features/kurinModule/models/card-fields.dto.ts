/** How the card treats one of its own fields in a край: asked for, offered, or left out. */
export type CardFieldMode = 'required' | 'optional' | 'hidden';

/** A field a край adds to the card, such as a visa type. Dates travel as «YYYY-MM-DD». */
export interface CardExtraFieldDto {
  key: string;
  label: string;
  type: 'text' | 'date';
  required: boolean;
}

/** The card fields of a kurin's край, from the kurin's profile. */
export interface CardFieldsDto {
  fields: Record<string, CardFieldMode>;
  extra: CardExtraFieldDto[];
}

/** A field a kurin may leave off its card: one its край makes optional, or adds without requiring. */
export interface KurinCardFieldDto {
  key: string;
  label: string;
  hidden: boolean;
}

/** The card as it is without a profile: name and phone asked for, nothing added. */
export const DEFAULT_CARD_FIELDS: CardFieldsDto = {
  fields: { middleName: 'required', phoneNumber: 'required' },
  extra: []
};

export function cardFieldMode(card: CardFieldsDto, field: 'middleName' | 'phoneNumber'): CardFieldMode {
  return card.fields[field] ?? DEFAULT_CARD_FIELDS.fields[field];
}

/** The край's own fields that have a value, in the край's order; a date reads «DD.MM.YYYY». */
export function cardExtraLines(card: CardFieldsDto, values: Record<string, string> | null | undefined): { key: string; label: string; value: string }[] {
  return card.extra
    .filter(field => !!values?.[field.key])
    .map(field => {
      const value = values![field.key];
      const date = field.type === 'date' ? /^(\d{4})-(\d{2})-(\d{2})$/.exec(value) : null;
      return { key: field.key, label: field.label, value: date ? `${date[3]}.${date[2]}.${date[1]}` : value };
    });
}
