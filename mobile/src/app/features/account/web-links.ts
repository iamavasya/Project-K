/**
 * Links out of the app: the docs site and the web's own pages (password reset, activation, the
 * join form), which the PWA does not repeat.
 */

/** Where the web's docs point when its env.js names none (the web's environment.prod.ts). */
const DEFAULT_DOCS_URL = 'https://docs-projectk.rostyslav-mukha.dev/';

/**
 * The docs site's root. The server's /env.js names it (`PROJECTK_DOCS_URL`) for the web and the
 * PWA alike, so a self-hosted install points both at its own docs.
 */
export function docsUrl(): string {
  const fromEnv = (globalThis.__PROJECTK_CONFIG__ as { docsUrl?: string } | undefined)?.docsUrl;
  return fromEnv && !fromEnv.includes('${') ? fromEnv : DEFAULT_DOCS_URL;
}

/** «Довідка» opens the guide's first page, like the web's sidebar. */
export function helpUrl(): string {
  return `${docsUrl().replace(/\/+$/, '')}/user/start/what-is/`;
}

/** A page of the web app on this origin: the PWA lives under /m/ next to it. */
export function webPage(path: string): string {
  return new URL(path, globalThis.location?.origin ?? 'http://localhost').href;
}

/** Opens a page outside the app: a browser tab, or the system's in-app browser from the home screen. */
export function openExternal(url: string): void {
  globalThis.open?.(url, '_blank', 'noopener');
}
