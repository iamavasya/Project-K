// Anonymous pings from Lilejka instances (self-host bundles and prod). Numbers and codes only:
// a random instance id, the release, counts. No names, emails, addresses or IPs are stored; the
// country is the one Cloudflare derives from the request.

import { dayOf } from "./checks";
import type { Env } from "./env";
import { notify } from "./telegram";

export const MAX_PING_BYTES = 2048;
/** New instances accepted per UTC day; a flood of fake ids stops here instead of eating D1. */
export const MAX_NEW_INSTANCES_PER_DAY = 200;
const MAX_COUNT = 10_000_000;

export interface Ping {
  instanceId: string;
  version: string;
  kind: string;
  krais: string[];
  kurins: number;
  members: number;
  activeUsers7d: number;
  activeUsers30d: number;
  requests24h: number;
  errors24h: number;
  uptimeHours: number;
}

const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/;
const VERSION = /^[0-9A-Za-z][0-9A-Za-z.+_-]{0,39}$/;
const KIND = /^[a-z][a-z0-9-]{0,19}$/;
const KRAI = /^[a-z]{2,8}$/;
const COUNTS = ["kurins", "members", "activeUsers7d", "activeUsers30d", "requests24h", "errors24h", "uptimeHours"] as const;

export function parsePing(input: unknown): { ping: Ping } | { error: string } {
  if (typeof input !== "object" || input === null || Array.isArray(input)) return { error: "очікую JSON-об'єкт" };
  const o = input as Record<string, unknown>;
  const id = typeof o.instanceId === "string" ? o.instanceId.toLowerCase() : "";
  if (!UUID.test(id)) return { error: "instanceId: потрібен UUID" };
  if (typeof o.version !== "string" || !VERSION.test(o.version)) return { error: "version: до 40 символів [0-9A-Za-z.+_-]" };
  const kind = typeof o.kind === "string" ? o.kind.toLowerCase() : "";
  if (!KIND.test(kind)) return { error: "kind: наприклад selfhost або prod" };
  const krais = o.krais ?? [];
  if (!Array.isArray(krais) || krais.length > 20 || !krais.every((k) => typeof k === "string" && KRAI.test(k))) return { error: "krais: до 20 кодів краю" };

  const counts = {} as Record<(typeof COUNTS)[number], number>;
  for (const key of COUNTS) {
    const v = o[key] ?? 0;
    if (typeof v !== "number" || !Number.isInteger(v) || v < 0 || v > MAX_COUNT) return { error: `${key}: ціле від 0 до ${MAX_COUNT}` };
    counts[key] = v;
  }
  return { ping: { instanceId: id, version: o.version, kind, krais: [...new Set(krais as string[])].sort((a, b) => a.localeCompare(b)), ...counts } };
}

const meta = (env: Env, key: string) => env.DB.prepare("SELECT value FROM meta WHERE key = ?").bind(key).first<string>("value");

export async function latestVersion(env: Env): Promise<string | null> {
  return meta(env, "latest_version");
}

export type PingOutcome = { status: 200; latest: string | null } | { status: 429 };

export async function recordPing(env: Env, ping: Ping, country: string | null, now: number): Promise<PingOutcome> {
  const day = dayOf(now);
  const existing = await env.DB.prepare("SELECT silent_alerted FROM instances WHERE id = ?").bind(ping.instanceId).first<{ silent_alerted: number }>();
  const statements: D1PreparedStatement[] = [];

  if (!existing) {
    const key = `new_instances:${day}`;
    const today = Number((await meta(env, key)) ?? 0);
    if (today >= MAX_NEW_INSTANCES_PER_DAY) return { status: 429 };
    statements.push(
      env.DB.prepare("INSERT INTO meta (key, value) VALUES (?1, '1') ON CONFLICT (key) DO UPDATE SET value = CAST(value AS INTEGER) + 1").bind(key),
    );
  }

  statements.push(
    env.DB
      .prepare(
        `INSERT INTO instances (id, first_seen, last_seen, version, kind, country, krais, kurins, members, active_7d, active_30d, requests_24h, errors_24h, uptime_hours, silent_alerted)
         VALUES (?1, ?2, ?2, ?3, ?4, ?5, ?6, ?7, ?8, ?9, ?10, ?11, ?12, ?13, 0)
         ON CONFLICT (id) DO UPDATE SET last_seen = excluded.last_seen, version = excluded.version, kind = excluded.kind,
           country = COALESCE(excluded.country, instances.country), krais = excluded.krais, kurins = excluded.kurins, members = excluded.members,
           active_7d = excluded.active_7d, active_30d = excluded.active_30d, requests_24h = excluded.requests_24h,
           errors_24h = excluded.errors_24h, uptime_hours = excluded.uptime_hours, silent_alerted = 0`,
      )
      .bind(
        ping.instanceId,
        now,
        ping.version,
        ping.kind,
        country,
        ping.krais.join(","),
        ping.kurins,
        ping.members,
        ping.activeUsers7d,
        ping.activeUsers30d,
        ping.requests24h,
        ping.errors24h,
        ping.uptimeHours,
      ),
    env.DB
      .prepare(
        `INSERT INTO instance_days (instance, day, version, kind, members, active_7d, requests_24h, errors_24h) VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8)
         ON CONFLICT (instance, day) DO UPDATE SET version = excluded.version, kind = excluded.kind, members = excluded.members,
           active_7d = excluded.active_7d, requests_24h = excluded.requests_24h, errors_24h = excluded.errors_24h`,
      )
      .bind(ping.instanceId, day, ping.version, ping.kind, ping.members, ping.activeUsers7d, ping.requests24h, ping.errors24h),
  );
  await env.DB.batch(statements);

  if (existing?.silent_alerted) await notify(env, `🟢 Інстанс ${describe(ping.instanceId, ping.krais, country)} знову пінгує (${ping.version})`);
  return { status: 200, latest: await latestVersion(env) };
}

const describe = (id: string, krais: string[] | string, country: string | null) => {
  const k = Array.isArray(krais) ? krais.join(",") : krais;
  return `${id.slice(0, 8)}${k ? ` [${k}]` : ""}${country ? ` ${country}` : ""}`;
};

export const SILENT_AFTER_MS = 3 * 86_400_000;

/** Self-hosts and prod that stopped pinging: one alert each until they come back. */
export async function alertSilent(env: Env, now: number): Promise<number> {
  const { results } = await env.DB
    .prepare("SELECT id, version, krais, country, last_seen FROM instances WHERE silent_alerted = 0 AND last_seen < ? AND kind <> 'dev' ORDER BY last_seen")
    .bind(now - SILENT_AFTER_MS)
    .all<{ id: string; version: string; krais: string; country: string | null; last_seen: number }>();
  if (results.length === 0) return 0;
  const lines = results.slice(0, 20).map((r) => `• ${describe(r.id, r.krais, r.country)}, ${r.version}, востаннє ${dayOf(r.last_seen)}`);
  if (results.length > 20) lines.push(`…і ще ${results.length - 20}`);
  await notify(env, `🟡 Замовкли інстанси (понад 3 дні без пінгу):\n${lines.join("\n")}`);
  await env.DB.batch(results.map((r) => env.DB.prepare("UPDATE instances SET silent_alerted = 1 WHERE id = ?").bind(r.id)));
  return results.length;
}

/** Everything /admin shows. Live = pinged in the last 7 days. */
export async function telemetrySummary(env: Env, now: number) {
  const live = now - 7 * 86_400_000;
  const firstDay = dayOf(now - 89 * 86_400_000);
  const [totals, byVersion, byKind, byCountry, krais, instances, trend] = await env.DB.batch([
    env.DB
      .prepare(
        `SELECT COUNT(*) AS instances, COALESCE(SUM(kurins), 0) AS kurins, COALESCE(SUM(members), 0) AS members,
           COALESCE(SUM(active_7d), 0) AS active7d, COALESCE(SUM(active_30d), 0) AS active30d,
           COALESCE(SUM(requests_24h), 0) AS requests24h, COALESCE(SUM(errors_24h), 0) AS errors24h
         FROM instances WHERE last_seen >= ?`,
      )
      .bind(live),
    env.DB.prepare("SELECT version, COUNT(*) AS count, SUM(members) AS members FROM instances WHERE last_seen >= ? GROUP BY version ORDER BY count DESC").bind(live),
    env.DB.prepare("SELECT kind, COUNT(*) AS count, SUM(members) AS members FROM instances WHERE last_seen >= ? GROUP BY kind ORDER BY count DESC").bind(live),
    env.DB.prepare("SELECT COALESCE(country, '??') AS country, COUNT(*) AS count FROM instances WHERE last_seen >= ? GROUP BY country ORDER BY count DESC").bind(live),
    env.DB.prepare("SELECT krais FROM instances WHERE last_seen >= ?").bind(live),
    env.DB.prepare("SELECT * FROM instances ORDER BY last_seen DESC LIMIT 500"),
    env.DB
      .prepare(
        `SELECT day, COUNT(*) AS instances, SUM(members) AS members, SUM(active_7d) AS active7d, SUM(requests_24h) AS requests24h, SUM(errors_24h) AS errors24h
         FROM instance_days WHERE day >= ? GROUP BY day ORDER BY day`,
      )
      .bind(firstDay),
  ]);

  const kraiCount = new Map<string, number>();
  for (const row of krais.results as { krais: string }[]) for (const k of row.krais.split(",").filter(Boolean)) kraiCount.set(k, (kraiCount.get(k) ?? 0) + 1);

  return {
    generatedAt: now,
    latest: await latestVersion(env),
    totals: totals.results[0],
    byVersion: byVersion.results,
    byKind: byKind.results,
    byCountry: byCountry.results,
    byKrai: [...kraiCount].map(([krai, count]) => ({ krai, count })).sort((a, b) => b.count - a.count),
    instances: (instances.results as Record<string, unknown>[]).map((r) => ({ ...r, live: (r.last_seen as number) >= live })),
    trend: trend.results,
  };
}
