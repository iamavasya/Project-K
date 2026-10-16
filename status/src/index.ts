import { Hono, type Context } from "hono";
import { cors } from "hono/cors";
import { authenticate } from "./auth";
import { runChecks } from "./checks";
import { runCron } from "./cron";
import { type Env, num } from "./env";
import { buildStatus, type IncidentRow, toIncident } from "./status";
import { MAX_PING_BYTES, parsePing, recordPing, telemetrySummary } from "./telemetry";

type App = { Bindings: Env; Variables: { email: string } };
const app = new Hono<App>();

const error = (c: Context<App>, status: 400 | 401 | 403 | 404 | 413 | 429, message: string) => c.json({ error: message }, status);

app.get("/health", (c) => c.json({ status: "ready" }));

// ---- public ---------------------------------------------------------------------------------

app.use("/v1/status", cors({ origin: "*", allowMethods: ["GET", "HEAD", "OPTIONS"], maxAge: 86400 }));

/** One D1 read per minute at most, however many people keep the page open. */
const statusCacheKey = (c: Context<App>) => new Request(`${new URL(c.req.url).origin}/v1/status`);

app.get("/v1/status", async (c) => {
  const seconds = num(c.env.STATUS_CACHE_SECONDS, 60);
  const key = statusCacheKey(c);
  if (seconds > 0) {
    const hit = await caches.default.match(key);
    if (hit) return hit;
  }
  const res = new Response(JSON.stringify(await buildStatus(c.env, Date.now())), {
    headers: { "Content-Type": "application/json; charset=utf-8", "Cache-Control": `public, max-age=${seconds}` },
  });
  if (seconds > 0) c.executionCtx.waitUntil(caches.default.put(key, res.clone()));
  return res;
});

app.post("/v1/ping", async (c) => {
  const declared = Number(c.req.header("Content-Length") ?? 0);
  if (declared > MAX_PING_BYTES) return error(c, 413, `пінг більший за ${MAX_PING_BYTES} байт`);
  const text = await c.req.text();
  if (text.length > MAX_PING_BYTES) return error(c, 413, `пінг більший за ${MAX_PING_BYTES} байт`);

  let body: unknown;
  try {
    body = JSON.parse(text);
  } catch {
    return error(c, 400, "очікую JSON");
  }
  const parsed = parsePing(body);
  if ("error" in parsed) return error(c, 400, parsed.error);

  const country = (c.req.raw as Request & { cf?: { country?: string } }).cf?.country ?? null;
  const outcome = await recordPing(c.env, parsed.ping, country && /^[A-Z]{2}$/.test(country) ? country : null, Date.now());
  if (outcome.status === 429) return error(c, 429, "забагато нових інстансів сьогодні");
  return c.json({ ok: true, latest: outcome.latest });
});

// ---- admin, behind Cloudflare Access ------------------------------------------------------------

app.use("/v1/admin/*", async (c, next) => {
  const email = await authenticate(c.req.raw, c.env);
  if (!email) return error(c, c.req.header("Cf-Access-Jwt-Assertion") ? 403 : 401, "потрібен вхід через Cloudflare Access");
  c.set("email", email);
  await next();
});

app.get("/v1/admin/me", (c) => c.json({ email: c.get("email") }));

app.get("/v1/admin/telemetry", async (c) => c.json(await telemetrySummary(c.env, Date.now())));

app.delete("/v1/admin/instances/:id", async (c) => {
  const id = c.req.param("id").toLowerCase();
  const res = await c.env.DB.batch([
    c.env.DB.prepare("DELETE FROM instance_days WHERE instance = ?").bind(id),
    c.env.DB.prepare("DELETE FROM instances WHERE id = ?").bind(id),
  ]);
  if (res[1].meta.changes === 0) return error(c, 404, "такого інстансу немає");
  return c.body(null, 204);
});

/** Runs the checks right away, e.g. after a deploy, instead of waiting for the next tick. */
app.post("/v1/admin/check", async (c) => {
  const results = await runChecks(c.env, Date.now());
  await caches.default.delete(statusCacheKey(c));
  return c.json({ results });
});

const KINDS = ["incident", "maintenance", "notice"] as const;

interface IncidentInput {
  kind: (typeof KINDS)[number];
  title: string;
  body: string;
  targets: string[];
  startsAt: number;
  resolvedAt: number | null;
}

function parseIncident(input: unknown, env: Env, base?: IncidentInput): IncidentInput | string {
  if (typeof input !== "object" || input === null) return "очікую JSON-об'єкт";
  const o = { ...base, ...(input as Partial<IncidentInput>) };
  if (!KINDS.includes(o.kind as IncidentInput["kind"])) return "kind: incident, maintenance або notice";
  const title = typeof o.title === "string" ? o.title.trim() : "";
  if (title.length === 0 || title.length > 140) return "title: від 1 до 140 символів";
  const body = typeof o.body === "string" ? o.body.trim() : "";
  if (body.length > 4000) return "body: до 4000 символів";
  const ids = new Set((env.TARGETS ?? []).map((t) => t.id));
  const targets = o.targets ?? [];
  if (!Array.isArray(targets) || !targets.every((t) => typeof t === "string" && ids.has(t))) return "targets: лише id із TARGETS";
  const startsAt = o.startsAt ?? Date.now();
  if (typeof startsAt !== "number" || !Number.isFinite(startsAt)) return "startsAt: мітка часу в мс";
  const resolvedAt = o.resolvedAt ?? null;
  if (resolvedAt !== null && (typeof resolvedAt !== "number" || !Number.isFinite(resolvedAt))) return "resolvedAt: мітка часу в мс або null";
  return { kind: o.kind as IncidentInput["kind"], title, body, targets: [...new Set(targets)], startsAt, resolvedAt };
}

app.get("/v1/admin/incidents", async (c) => {
  const { results } = await c.env.DB.prepare("SELECT * FROM incidents ORDER BY starts_at DESC LIMIT 100").all<IncidentRow>();
  return c.json(results.map(toIncident));
});

app.post("/v1/admin/incidents", async (c) => {
  const input = parseIncident(await c.req.json().catch(() => null), c.env);
  if (typeof input === "string") return error(c, 400, input);
  const row = await c.env.DB
    .prepare(
      "INSERT INTO incidents (kind, title, body, targets, starts_at, resolved_at, created_by, updated_at) VALUES (?, ?, ?, ?, ?, ?, ?, ?) RETURNING *",
    )
    .bind(input.kind, input.title, input.body, input.targets.join(","), input.startsAt, input.resolvedAt, c.get("email"), Date.now())
    .first<IncidentRow>();
  await caches.default.delete(statusCacheKey(c));
  return c.json(toIncident(row!), 201);
});

app.patch("/v1/admin/incidents/:id", async (c) => {
  const id = Number(c.req.param("id"));
  const current = await c.env.DB.prepare("SELECT * FROM incidents WHERE id = ?").bind(id).first<IncidentRow>();
  if (!current) return error(c, 404, "такого запису немає");
  const was = toIncident(current);
  const input = parseIncident(await c.req.json().catch(() => null), c.env, { ...was });
  if (typeof input === "string") return error(c, 400, input);
  const row = await c.env.DB
    .prepare("UPDATE incidents SET kind = ?, title = ?, body = ?, targets = ?, starts_at = ?, resolved_at = ?, updated_at = ? WHERE id = ? RETURNING *")
    .bind(input.kind, input.title, input.body, input.targets.join(","), input.startsAt, input.resolvedAt, Date.now(), id)
    .first<IncidentRow>();
  await caches.default.delete(statusCacheKey(c));
  return c.json(toIncident(row!));
});

app.delete("/v1/admin/incidents/:id", async (c) => {
  const res = await c.env.DB.prepare("DELETE FROM incidents WHERE id = ?").bind(Number(c.req.param("id"))).run();
  if (res.meta.changes === 0) return error(c, 404, "такого запису немає");
  await caches.default.delete(statusCacheKey(c));
  return c.body(null, 204);
});

app.notFound((c) => error(c, 404, "не знайдено"));

export default {
  fetch: app.fetch,
  scheduled(event, env, ctx) {
    ctx.waitUntil(runCron(env, event.scheduledTime));
  },
} satisfies ExportedHandler<Env>;
