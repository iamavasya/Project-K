import { env } from "cloudflare:test";
import { exports } from "cloudflare:workers";
import { vi } from "vitest";

const b64url = (bytes: Uint8Array | string) =>
  btoa(typeof bytes === "string" ? bytes : String.fromCharCode(...bytes)).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");

/** A Cf-Access-Jwt-Assertion as Cloudflare Access would send it for this email. */
export async function accessToken(email: string): Promise<string> {
  const jwk = JSON.parse(env.TEST_PRIVATE_JWK) as JsonWebKey & { kid: string };
  const key = await crypto.subtle.importKey("jwk", jwk, { name: "RSASSA-PKCS1-v1_5", hash: "SHA-256" }, false, ["sign"]);
  const header = b64url(JSON.stringify({ alg: "RS256", kid: jwk.kid }));
  const payload = b64url(JSON.stringify({ aud: [env.ACCESS_AUD], iss: env.ACCESS_TEAM_DOMAIN, email, exp: Math.floor(Date.now() / 1000) + 600 }));
  const signature = new Uint8Array(await crypto.subtle.sign("RSASSA-PKCS1-v1_5", key, new TextEncoder().encode(`${header}.${payload}`)));
  return `${header}.${payload}.${b64url(signature)}`;
}

export async function call(path: string, init: RequestInit & { as?: string; json?: unknown } = {}): Promise<Response> {
  const headers = new Headers(init.headers);
  if (init.as) headers.set("Cf-Access-Jwt-Assertion", await accessToken(init.as));
  let body = init.body;
  if (init.json !== undefined) {
    headers.set("Content-Type", "application/json");
    body = JSON.stringify(init.json);
  }
  return (exports as unknown as { default: Fetcher }).default.fetch(new Request(`https://status.example${path}`, { ...init, headers, body }));
}

export const OWNER = "owner@example.com";

type Handler = (url: string, init?: RequestInit) => Response | Promise<Response>;

/** Replaces outbound fetch; returns the list of URLs (and bodies) the code sent. */
export function mockFetch(handler: Handler) {
  const sent: { url: string; body?: string }[] = [];
  vi.spyOn(globalThis, "fetch").mockImplementation(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = input instanceof Request ? input.url : String(input);
    sent.push({ url, body: typeof init?.body === "string" ? init.body : undefined });
    return handler(url, init);
  });
  return sent;
}

export const json = (value: unknown, status = 200) => new Response(JSON.stringify(value), { status, headers: { "Content-Type": "application/json" } });

export const ping = (overrides: Record<string, unknown> = {}) => ({
  instanceId: "3f2a9c1e-5b7d-4e8a-9c21-7d4e5f6a8b90",
  version: "v1.2.0",
  kind: "selfhost",
  krais: ["cz"],
  kurins: 2,
  members: 130,
  activeUsers7d: 12,
  activeUsers30d: 25,
  requests24h: 4200,
  errors24h: 3,
  uptimeHours: 71,
  ...overrides,
});
