import type { Target } from "./checks";

export interface Env {
  DB: D1Database;
  ASSETS: Fetcher;

  /** What the cron checks, in page order. */
  TARGETS: Target[];

  /** "https://<team>.cloudflareaccess.com" */
  ACCESS_TEAM_DOMAIN: string;
  /** Application Audience (AUD) tag of the Access application guarding /admin and /v1/admin. */
  ACCESS_AUD: string;
  /** Comma-separated emails allowed into /admin. */
  OWNER_EMAILS?: string;

  /** "owner/repo" whose latest GitHub release is handed back to pinging instances. */
  RELEASES_REPO?: string;
  /** Secret; alerts are off while it is empty. */
  TELEGRAM_BOT_TOKEN?: string;
  TELEGRAM_CHAT_ID?: string;

  /** Thresholds, as strings because vars may come from .dev.vars. */
  SLOW_MS?: string;
  TIMEOUT_MS?: string;
  /** How long /v1/status is kept in the edge cache; "0" turns the cache off (tests). */
  STATUS_CACHE_SECONDS?: string;

  /** "dev" only under `wrangler dev`, together with DEV_ACCESS_EMAIL in .dev.vars. */
  ENVIRONMENT?: string;
  DEV_ACCESS_EMAIL?: string;
  /** Tests only: a JWKS document used instead of fetching Access certs. */
  ACCESS_JWKS?: string;
}

export const num = (value: string | undefined, fallback: number) => {
  const n = Number(value);
  return value !== undefined && value !== "" && Number.isFinite(n) ? n : fallback;
};
