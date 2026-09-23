/**
 * The code name to show beside a version, or `null` when there is none to show.
 *
 * Releases used to be named `vX.Y.Z "Code Name"` and the build stamped both halves into the
 * environment file. Since 1.0 the code name is optional, so this can legitimately arrive empty —
 * and nothing should then print a bare pair of quotes after the version. A local or staging build
 * carries a placeholder instead (`LocalDevelopment`, `TailscaleDevelopment`): useful to whoever
 * runs it, noise to everyone else, so it is hidden as well.
 */
export function displayCodeName(codeName: string | null | undefined): string | null {
  const value = codeName?.trim();

  return !value || /development/i.test(value) ? null : value;
}
