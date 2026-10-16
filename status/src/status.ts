// What the public page reads: current state per target, 90 daily bars, 24 hourly averages,
// open and recent notices, and the automatic history of state changes.

import { dayOf, hourOf, type State } from "./checks";
import type { Env } from "./env";

export type BarState = State | "none";

/** A day (or hour) is red from 1 % of failed checks (~15 min a day), yellow on any failure or
 * when 5 % of checks were slow. */
export function barState(checks: number, degraded: number, down: number): BarState {
  if (checks === 0) return "none";
  if (down / checks >= 0.01) return "down";
  if (down > 0 || degraded / checks >= 0.05) return "degraded";
  return "up";
}

const avg = (sum: number, checks: number, down: number) => (checks - down > 0 ? Math.round(sum / (checks - down)) : null);

interface Counter {
  target: string;
  checks: number;
  degraded: number;
  down: number;
  latency_sum: number;
}

export interface Incident {
  id: number;
  kind: "incident" | "maintenance" | "notice";
  title: string;
  body: string;
  targets: string[];
  startsAt: number;
  resolvedAt: number | null;
}

export interface IncidentRow {
  id: number;
  kind: Incident["kind"];
  title: string;
  body: string;
  targets: string;
  starts_at: number;
  resolved_at: number | null;
  created_by: string;
  updated_at: number;
}

export const toIncident = (row: IncidentRow): Incident => ({
  id: row.id,
  kind: row.kind,
  title: row.title,
  body: row.body,
  targets: row.targets.split(",").filter(Boolean),
  startsAt: row.starts_at,
  resolvedAt: row.resolved_at,
});

export const DAYS = 90;
const HOURS = 24;
const HISTORY_DAYS = 14;

export async function buildStatus(env: Env, now: number) {
  const targets = env.TARGETS ?? [];
  const firstDay = dayOf(now - (DAYS - 1) * 86_400_000);
  const firstHour = hourOf(now) - (HOURS - 1);
  const since = now - HISTORY_DAYS * 86_400_000;

  const [states, days, hours, incidents, events] = await env.DB.batch([
    env.DB.prepare("SELECT target, state, since, checked_at, latency_ms, version, detail FROM target_state"),
    env.DB.prepare("SELECT target, day, checks, degraded, down, latency_sum FROM target_days WHERE day >= ?").bind(firstDay),
    env.DB.prepare("SELECT target, hour, checks, degraded, down, latency_sum FROM target_hours WHERE hour >= ?").bind(firstHour),
    // Open ones, upcoming maintenance, and whatever was resolved inside the history window.
    env.DB.prepare("SELECT * FROM incidents WHERE resolved_at IS NULL OR resolved_at >= ? ORDER BY starts_at DESC LIMIT 50").bind(since),
    env.DB.prepare("SELECT target, at, state, previous, detail FROM target_events WHERE at >= ? ORDER BY at DESC LIMIT 50").bind(since),
  ]);

  type StateRow = { target: string; state: State; since: number; checked_at: number; latency_ms: number | null; version: string | null; detail: string | null };
  const stateBy = new Map((states.results as StateRow[]).map((r) => [r.target, r]));
  const dayBy = new Map((days.results as (Counter & { day: string })[]).map((r) => [`${r.target}|${r.day}`, r]));
  const hourBy = new Map((hours.results as (Counter & { hour: number })[]).map((r) => [`${r.target}|${r.hour}`, r]));

  const dayList = Array.from({ length: DAYS }, (_, i) => dayOf(now - (DAYS - 1 - i) * 86_400_000));
  const hourList = Array.from({ length: HOURS }, (_, i) => firstHour + i);
  const openIncidents = (incidents.results as IncidentRow[]).map(toIncident);
  const maintenanceNow = openIncidents.filter((i) => i.kind === "maintenance" && i.resolvedAt === null && i.startsAt <= now);

  const items = targets.map((t) => {
    const s = stateBy.get(t.id);
    let checks = 0;
    let down = 0;
    const bars = dayList.map((day) => {
      const c = dayBy.get(`${t.id}|${day}`);
      if (!c) return { day, state: "none" as BarState, uptime: null, avgMs: null };
      checks += c.checks;
      down += c.down;
      return { day, state: barState(c.checks, c.degraded, c.down), uptime: (c.checks - c.down) / c.checks, avgMs: avg(c.latency_sum, c.checks, c.down) };
    });
    const hourly = hourList.map((hour) => {
      const c = hourBy.get(`${t.id}|${hour}`);
      return c
        ? { at: hour * 3_600_000, state: barState(c.checks, c.degraded, c.down), avgMs: avg(c.latency_sum, c.checks, c.down) }
        : { at: hour * 3_600_000, state: "none" as BarState, avgMs: null };
    });
    const inMaintenance = maintenanceNow.some((m) => m.targets.length === 0 || m.targets.includes(t.id));
    return {
      id: t.id,
      name: t.name,
      state: (s?.state ?? "none") as BarState,
      maintenance: inMaintenance,
      since: s?.since ?? null,
      checkedAt: s?.checked_at ?? null,
      latencyMs: s?.latency_ms ?? null,
      version: s?.version ?? null,
      detail: s?.detail ?? null,
      uptime: checks > 0 ? (checks - down) / checks : null,
      days: bars,
      hours: hourly,
    };
  });

  const known = items.filter((i) => i.state !== "none");
  const overall: BarState | "maintenance" =
    known.length === 0
      ? "none"
      : known.some((i) => i.state === "down" && !i.maintenance)
        ? "down"
        : known.some((i) => i.maintenance)
          ? "maintenance"
          : known.some((i) => i.state !== "up")
            ? "degraded"
            : "up";

  const names = new Map(targets.map((t) => [t.id, t.name]));
  return {
    generatedAt: now,
    overall,
    targets: items,
    incidents: openIncidents,
    events: (events.results as { target: string; at: number; state: State; previous: State | null; detail: string | null }[])
      .filter((e) => names.has(e.target) && e.previous !== null)
      .map((e) => ({ target: e.target, name: names.get(e.target), at: e.at, state: e.state, previous: e.previous, detail: e.detail })),
  };
}
