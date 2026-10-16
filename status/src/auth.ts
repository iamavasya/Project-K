// Who is calling. Cloudflare Access sits in front of /admin and /v1/admin and signs the owner in;
// it forwards a signed JWT in Cf-Access-Jwt-Assertion. The Worker verifies that JWT itself (a
// request that reaches the Worker some other way must not pass), then checks the email against
// OWNER_EMAILS. Same scheme as the catalog worker.

import type { Env } from "./env";

interface Jwk extends JsonWebKey {
  kid: string;
}

let jwksCache: { keys: Jwk[]; until: number } | null = null;

async function loadKeys(env: Env): Promise<Jwk[]> {
  if (env.ACCESS_JWKS) return (JSON.parse(env.ACCESS_JWKS) as { keys: Jwk[] }).keys;
  if (jwksCache && jwksCache.until > Date.now()) return jwksCache.keys;
  const res = await fetch(`${env.ACCESS_TEAM_DOMAIN}/cdn-cgi/access/certs`);
  if (!res.ok) throw new Error(`Access certs: HTTP ${res.status}`);
  const { keys } = (await res.json()) as { keys: Jwk[] };
  jwksCache = { keys, until: Date.now() + 60 * 60 * 1000 };
  return keys;
}

function b64url(input: string): Uint8Array {
  const s = input.replace(/-/g, "+").replace(/_/g, "/").padEnd(Math.ceil(input.length / 4) * 4, "=");
  return Uint8Array.from(atob(s), (c) => c.charCodeAt(0));
}

/** Returns the verified email, or null when the token is missing, forged, expired or meant
 * for another application. */
export async function verifyAccessJwt(token: string, env: Env, now = Date.now()): Promise<string | null> {
  const parts = token.split(".");
  if (parts.length !== 3) return null;
  try {
    const header = JSON.parse(new TextDecoder().decode(b64url(parts[0]))) as { alg: string; kid: string };
    if (header.alg !== "RS256") return null;
    const jwk = (await loadKeys(env)).find((k) => k.kid === header.kid);
    if (!jwk) return null;

    const key = await crypto.subtle.importKey("jwk", jwk, { name: "RSASSA-PKCS1-v1_5", hash: "SHA-256" }, false, ["verify"]);
    const ok = await crypto.subtle.verify("RSASSA-PKCS1-v1_5", key, b64url(parts[2]), new TextEncoder().encode(`${parts[0]}.${parts[1]}`));
    if (!ok) return null;

    const claims = JSON.parse(new TextDecoder().decode(b64url(parts[1]))) as { aud: string | string[]; exp: number; nbf?: number; iss: string; email?: string };
    const aud = Array.isArray(claims.aud) ? claims.aud : [claims.aud];
    if (!aud.includes(env.ACCESS_AUD)) return null;
    if (claims.iss !== env.ACCESS_TEAM_DOMAIN) return null;
    if (claims.exp * 1000 < now || (claims.nbf && claims.nbf * 1000 > now + 60_000)) return null;
    return claims.email?.toLowerCase() ?? null;
  } catch {
    return null;
  }
}

/** The signed-in owner's email, or null. */
export async function authenticate(req: Request, env: Env): Promise<string | null> {
  let email: string | null = null;
  const token = req.headers.get("Cf-Access-Jwt-Assertion");
  if (token) {
    email = await verifyAccessJwt(token, env);
  } else if (env.ENVIRONMENT === "dev" && env.DEV_ACCESS_EMAIL && ["localhost", "127.0.0.1"].includes(new URL(req.url).hostname)) {
    // Local `wrangler dev` has no Access in front; .dev.vars names who you are.
    email = env.DEV_ACCESS_EMAIL.toLowerCase();
  }
  if (!email) return null;
  const owners = (env.OWNER_EMAILS ?? "").split(",").map((e) => e.trim().toLowerCase()).filter(Boolean);
  return owners.includes(email) ? email : null;
}
