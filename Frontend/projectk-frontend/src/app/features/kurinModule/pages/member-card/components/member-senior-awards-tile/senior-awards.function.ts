import { MemberAwardDto } from '../../../../models/member-award.dto';
import { CatalogHonour } from '../../../../models/honour.dto';

/** A senior відзначення as the card shows it. */
export interface SeniorAwardRow {
  award: MemberAwardDto;
  title: string;
  imageUrl: string;
}

/**
 * The person's senior відзначення, newest first, named and drawn from the catalogue. One the catalogue
 * no longer knows keeps its id for a name: it was recorded, it does not vanish.
 */
export function seniorAwardRows(awards: readonly MemberAwardDto[] | null | undefined, honours: readonly CatalogHonour[]): SeniorAwardRow[] {
  const byId = new Map(honours.map(item => [item.id, item]));
  return (awards ?? [])
    .filter(award => !!award.catalogId)
    .sort((a, b) => b.dateAcquired.localeCompare(a.dateAcquired))
    .map(award => {
      const item = byId.get(award.catalogId!);
      return { award, title: item?.title ?? award.catalogId!, imageUrl: item?.imageUrl ?? '' };
    });
}
