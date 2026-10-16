import { env } from "cloudflare:test";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { dayOf, hourOf, probe, runChecks } from "../src/checks";
import { housekeeping } from "../src/cron";
import { MAX_NEW_INSTANCES_PER_DAY } from "../src/telemetry";
import { call, json, mockFetch, OWNER, ping } from "./helpers";

const TABLES = ["target_state", "target_hours", "target_days", "target_events", "incidents", "instances", "instance_days", "meta"];

async function body<T = any>(res: Response): Promise<T> {
  return (await res.json()) as T;
}

const telegram = (sent: { url: string; body?: string }[]) => sent.filter((s) => s.url.startsWith("https://api.telegram.org/")).map((s) => JSON.parse(s.body!).text as string);

beforeEach(async () => {
  await env.DB.batch(TABLES.map((t) => env.DB.prepare(`DELETE FROM ${t}`)));
});
afterEach(() => {
  vi.restoreAllMocks();
});

describe("probe", () => {
  const limits = { slowMs: 3000, timeoutMs: 10000 };
  const api = { id: "api", name: "API", url: "https://api.test/health", kind: "health" as const };
  const web = { id: "web", name: "Веб", url: "https://web.test/", kind: "http" as const };

  it("is up and reads the version from /health", async () => {
    mockFetch(() => json({ status: "ready", version: "v1.2.0" }));
    expect(await probe(api, limits)).toMatchObject({ state: "up", version: "v1.2.0", detail: null });
  });

  it("is degraded when /health is not ready", async () => {
    mockFetch(() => json({ status: "starting" }));
    expect(await probe(api, limits)).toMatchObject({ state: "degraded", detail: "health: starting" });
  });

  it("is degraded when slow", async () => {
    mockFetch(() => new Response("ok"));
    let t = 0;
    expect(await probe(web, limits, () => (t += 4000))).toMatchObject({ state: "degraded", latencyMs: 4000 });
  });

  it("is down on a 5xx", async () => {
    mockFetch(() => new Response("bad gateway", { status: 502 }));
    expect(await probe(web, limits)).toMatchObject({ state: "down", detail: "HTTP 502" });
  });

  it("is down on a network error", async () => {
    mockFetch(() => {
      throw new TypeError("connection refused");
    });
    expect(await probe(web, limits)).toMatchObject({ state: "down", latencyMs: null, detail: "помилка мережі" });
  });
});

describe("cron checks", () => {
  it("stores state, hour and day counters, and alerts only on a change to down", async () => {
    let apiUp = true;
    const sent = mockFetch((url) => {
      if (url.startsWith("https://api.telegram.org/")) return json({ ok: true });
      if (url === "https://api.test/health") return apiUp ? json({ status: "ready", version: "v1.2.0" }) : new Response("", { status: 503 });
      return new Response("ok");
    });
    const at = Date.parse("2026-10-16T10:00:00Z");

    await runChecks(env, at, 0);
    expect(telegram(sent)).toEqual([]);

    apiUp = false;
    await runChecks(env, at + 120_000, 0);
    expect(telegram(sent)).toEqual([expect.stringContaining("API лежить: HTTP 503")]);

    const state = await env.DB.prepare("SELECT * FROM target_state WHERE target = 'api'").first();
    // The version of the last answer that carried one survives the outage.
    expect(state).toMatchObject({ state: "down", since: at + 120_000, version: "v1.2.0" });
    const day = await env.DB.prepare("SELECT * FROM target_days WHERE target = 'api' AND day = ?").bind(dayOf(at)).first();
    expect(day).toMatchObject({ checks: 2, down: 1 });
    const hour = await env.DB.prepare("SELECT * FROM target_hours WHERE target = 'web' AND hour = ?").bind(hourOf(at)).first();
    expect(hour).toMatchObject({ checks: 2, down: 0 });

    apiUp = true;
    await runChecks(env, at + 240_000, 0);
    expect(telegram(sent).at(-1)).toContain("API знову відповідає");
    const { results: events } = await env.DB.prepare("SELECT state FROM target_events WHERE target = 'api' ORDER BY at").all();
    expect(events.map((e) => e.state)).toEqual(["up", "down", "up"]);
  });
});

describe("GET /v1/status", () => {
  it("lists every target with 90 day bars and 24 hour averages", async () => {
    const now = Date.now();
    await env.DB.batch([
      env.DB.prepare("INSERT INTO target_state VALUES ('api', 'up', ?1, ?1, 120, 'v1.2.0', NULL)").bind(now),
      env.DB.prepare("INSERT INTO target_days VALUES ('api', ?, 720, 0, 8, 85000)").bind(dayOf(now - 86_400_000)),
      env.DB.prepare("INSERT INTO target_days VALUES ('api', ?, 10, 0, 0, 1200)").bind(dayOf(now)),
      env.DB.prepare("INSERT INTO target_hours VALUES ('api', ?, 10, 0, 0, 1200)").bind(hourOf(now)),
    ]);
    const res = await call("/v1/status");
    expect(res.status).toBe(200);
    const status = await body(res);
    expect(status.overall).toBe("up");
    expect(status.targets.map((t: any) => t.id)).toEqual(["api", "web"]);
    const api = status.targets[0];
    expect(api).toMatchObject({ state: "up", version: "v1.2.0", latencyMs: 120 });
    expect(api.days).toHaveLength(90);
    expect(api.days.at(-2)).toMatchObject({ state: "down", avgMs: 119 });
    expect(api.days.at(-1)).toMatchObject({ state: "up", avgMs: 120 });
    expect(api.days[0].state).toBe("none");
    expect(api.uptime).toBeCloseTo(722 / 730);
    expect(api.hours).toHaveLength(24);
    expect(api.hours.at(-1)).toMatchObject({ state: "up", avgMs: 120 });
    expect(status.targets[1].state).toBe("none");
  });

  it("shows maintenance instead of an outage", async () => {
    const now = Date.now();
    await env.DB.batch([
      env.DB.prepare("INSERT INTO target_state VALUES ('api', 'down', ?1, ?1, NULL, NULL, 'HTTP 503')").bind(now),
      env.DB.prepare("INSERT INTO incidents (kind, title, targets, starts_at, created_by, updated_at) VALUES ('maintenance', 'Оновлення бази', 'api', ?1, 'x', ?1)").bind(now - 60_000),
    ]);
    const status = await body(await call("/v1/status"));
    expect(status.overall).toBe("maintenance");
    expect(status.targets[0].maintenance).toBe(true);
    expect(status.incidents[0]).toMatchObject({ kind: "maintenance", title: "Оновлення бази", targets: ["api"] });
  });
});

describe("POST /v1/ping", () => {
  it("stores the latest ping and one row per day, and returns the latest release", async () => {
    await env.DB.prepare("INSERT INTO meta VALUES ('latest_version', 'v1.3.0')").run();
    const res = await call("/v1/ping", { method: "POST", json: ping() });
    expect(res.status).toBe(200);
    expect(await body(res)).toEqual({ ok: true, latest: "v1.3.0" });

    await call("/v1/ping", { method: "POST", json: ping({ members: 131 }) });
    const instance = await env.DB.prepare("SELECT * FROM instances").first();
    expect(instance).toMatchObject({ version: "v1.2.0", kind: "selfhost", krais: "cz", members: 131 });
    const { results } = await env.DB.prepare("SELECT * FROM instance_days").all();
    expect(results).toHaveLength(1);
    expect(results[0]).toMatchObject({ members: 131 });
  });

  it("refuses junk", async () => {
    expect((await call("/v1/ping", { method: "POST", body: "nope" })).status).toBe(400);
    expect((await call("/v1/ping", { method: "POST", json: ping({ instanceId: "x" }) })).status).toBe(400);
    expect((await call("/v1/ping", { method: "POST", json: { ...ping(), pad: "x".repeat(3000) } })).status).toBe(413);
  });

  it("caps new instances per day but keeps known ones pinging", async () => {
    await call("/v1/ping", { method: "POST", json: ping() });
    await env.DB.prepare("UPDATE meta SET value = ? WHERE key LIKE 'new_instances:%'").bind(String(MAX_NEW_INSTANCES_PER_DAY)).run();
    const stranger = await call("/v1/ping", { method: "POST", json: ping({ instanceId: "11111111-2222-4333-8444-555555555555" }) });
    expect(stranger.status).toBe(429);
    expect((await call("/v1/ping", { method: "POST", json: ping() })).status).toBe(200);
  });
});

describe("silent instances", () => {
  it("alerts once when an instance stops pinging, and again when it is back", async () => {
    const sent = mockFetch((url) => (url.startsWith("https://api.telegram.org/") ? json({ ok: true }) : new Response("", { status: 404 })));
    await call("/v1/ping", { method: "POST", json: ping() });
    const later = Date.now() + 4 * 86_400_000;

    await housekeeping(env, later);
    await housekeeping(env, later + 86_400_000);
    expect(telegram(sent)).toEqual([expect.stringContaining("Замовкли інстанси")]);

    await call("/v1/ping", { method: "POST", json: ping() });
    expect(telegram(sent).at(-1)).toContain("знову пінгує");
  });
});

describe("admin", () => {
  it("needs Access", async () => {
    expect((await call("/v1/admin/telemetry")).status).toBe(401);
    expect((await call("/v1/admin/telemetry", { as: "stranger@example.com" })).status).toBe(403);
  });

  it("sums live instances", async () => {
    await call("/v1/ping", { method: "POST", json: ping() });
    await call("/v1/ping", { method: "POST", json: ping({ instanceId: "11111111-2222-4333-8444-555555555555", kind: "prod", krais: ["ua"], members: 400 }) });
    const summary = await body(await call("/v1/admin/telemetry", { as: OWNER }));
    expect(summary.totals).toMatchObject({ instances: 2, members: 530 });
    expect(summary.byKind).toEqual(expect.arrayContaining([expect.objectContaining({ kind: "prod", count: 1 })]));
    expect(summary.byKrai).toEqual(expect.arrayContaining([{ krai: "cz", count: 1 }, { krai: "ua", count: 1 }]));
    expect(summary.instances).toHaveLength(2);
    expect(summary.trend).toEqual([expect.objectContaining({ instances: 2, members: 530 })]);

    expect((await call(`/v1/admin/instances/${ping().instanceId}`, { method: "DELETE", as: OWNER })).status).toBe(204);
    expect((await body(await call("/v1/admin/telemetry", { as: OWNER }))).totals.instances).toBe(1);
  });

  it("creates, resolves and deletes incidents", async () => {
    const created = await call("/v1/admin/incidents", { method: "POST", as: OWNER, json: { kind: "incident", title: "Не працює вхід", targets: ["api"] } });
    expect(created.status).toBe(201);
    const incident = await body(created);
    expect(incident).toMatchObject({ kind: "incident", title: "Не працює вхід", targets: ["api"], resolvedAt: null });

    expect((await call("/v1/admin/incidents", { method: "POST", as: OWNER, json: { kind: "incident", title: "x", targets: ["nope"] } })).status).toBe(400);

    const resolved = await body(await call(`/v1/admin/incidents/${incident.id}`, { method: "PATCH", as: OWNER, json: { resolvedAt: Date.now() } }));
    expect(resolved).toMatchObject({ title: "Не працює вхід", resolvedAt: expect.any(Number) });
    expect((await body(await call("/v1/status"))).incidents).toHaveLength(1);

    expect((await call(`/v1/admin/incidents/${incident.id}`, { method: "DELETE", as: OWNER })).status).toBe(204);
    expect((await body(await call("/v1/admin/incidents", { as: OWNER })))).toEqual([]);
  });
});
