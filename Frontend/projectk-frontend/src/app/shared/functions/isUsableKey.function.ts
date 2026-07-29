const EMPTY_GUID = '00000000-0000-0000-0000-000000000000';

/**
 * Tells a real entity key from a placeholder one.
 *
 * The API serialises "no relation" as `Guid.Empty` rather than null, so a plain
 * truthiness check accepts it and the UI ends up linking to `/group/00000000-…`,
 * which the access checks reject.
 */
export function isUsableKey(key: string | null | undefined): boolean {
  const trimmed = key?.trim();
  return !!trimmed && trimmed.toLowerCase() !== EMPTY_GUID;
}
