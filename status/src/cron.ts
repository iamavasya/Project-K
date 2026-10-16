// The one cron trigger: checks every tick, housekeeping once a day on the 03:00 UTC tick.

import { dayOf, hourOf, runChecks } from "./checks";
import type { Env } from "./env";
import { alertSilent, latestVersion } from "./telemetry";

const DAY = 86_400_000;

export const isDailyTick = (at: number) => {
  const d = new Date(at);
  return d.getUTCHours() === 3 && d.getUTCMinutes() < 2;
};

export async function runCron(env: Env, at: number): Promise<void> {
  await runChecks(env, at);
  if (isDailyTick(at)) await housekeeping(env, at);
  // Right after the first deploy there is no known release yet; ask GitHub hourly until there is.
  else if (new Date(at).getUTCMinutes() < 2 && (await latestVersion(env)) === null) await refreshLatestVersion(env);
}

export async function housekeeping(env: Env, at: number): Promise<void> {
  await env.DB.batch([
    env.DB.prepare("DELETE FROM target_hours WHERE hour < ?").bind(hourOf(at) - 48),
    env.DB.prepare("DELETE FROM target_days WHERE day < ?").bind(dayOf(at - 400 * DAY)),
    env.DB.prepare("DELETE FROM instance_days WHERE day < ?").bind(dayOf(at - 400 * DAY)),
    env.DB.prepare("DELETE FROM target_events WHERE at < ?").bind(at - 90 * DAY),
    env.DB.prepare("DELETE FROM meta WHERE key LIKE 'new_instances:%' AND key < ?").bind(`new_instances:${dayOf(at - DAY)}`),
  ]);
  await refreshLatestVersion(env);
  await alertSilent(env, at);
}

/** "v1.10.2" > "v1.9.0"; anything that is not a plain release sorts below. */
export function compareVersions(a: string, b: string): number {
  const parse = (v: string) => (/^v?(\d+)\.(\d+)\.(\d+)$/.exec(v)?.slice(1).map(Number) ?? null);
  const x = parse(a);
  const y = parse(b);
  if (!x || !y) return x ? 1 : y ? -1 : 0;
  for (let i = 0; i < 3; i++) if (x[i] !== y[i]) return x[i] - y[i];
  return 0;
}

/** The newest release on GitHub, or the highest plain vX.Y.Z tag when the repo has no releases. */
export async function refreshLatestVersion(env: Env): Promise<string | null> {
  if (!env.RELEASES_REPO) return null;
  const get = (path: string) =>
    fetch(`https://api.github.com/repos/${env.RELEASES_REPO}${path}`, {
      headers: { "User-Agent": "projectk-status", Accept: "application/vnd.github+json" },
    });
  let latest: string | null = null;
  try {
    const release = await get("/releases/latest");
    if (release.ok) latest = ((await release.json()) as { tag_name?: string }).tag_name ?? null;
    if (!latest) {
      const tags = await get("/tags?per_page=100");
      if (tags.ok) {
        const names = ((await tags.json()) as { name: string }[]).map((t) => t.name).filter((n) => /^v?\d+\.\d+\.\d+$/.test(n));
        names.sort(compareVersions);
        latest = names.at(-1) ?? null;
      }
    }
  } catch (e) {
    console.error("latest version:", e);
  }
  if (latest) {
    await env.DB.prepare("INSERT INTO meta (key, value) VALUES ('latest_version', ?1) ON CONFLICT (key) DO UPDATE SET value = ?1").bind(latest.slice(0, 40)).run();
  }
  return latest;
}
