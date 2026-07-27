export const TITLE_SEPARATOR = ' · ';

/** Route data key that tells the title strategy which entity to resolve. */
export type TitleContextType = 'kurin' | 'group' | 'member';

export function formatKurinTitle(kurinNumber: number | null | undefined): string | null {
  return typeof kurinNumber === 'number' && Number.isFinite(kurinNumber)
    ? `к. ч. ${kurinNumber}`
    : null;
}

export function formatGroupTitle(name: string | null | undefined): string | null {
  const trimmed = name?.trim();
  return trimmed ? `г. ${trimmed}` : null;
}

export function formatMemberTitle(
  lastName: string | null | undefined,
  firstName: string | null | undefined
): string | null {
  const parts = [lastName?.trim(), firstName?.trim()].filter(
    (part): part is string => !!part
  );

  return parts.length > 0 ? parts.join(' ') : null;
}

export function composePageTitle(context: string | null, appName: string): string {
  const trimmedContext = context?.trim();
  return trimmedContext ? `${trimmedContext}${TITLE_SEPARATOR}${appName}` : appName;
}
