import { describe, expect, it } from "vitest";
import { alertText, type Target } from "../src/checks";
import { compareVersions, isDailyTick } from "../src/cron";
import { barState } from "../src/status";
import { parsePing } from "../src/telemetry";
import { ping } from "./helpers";

describe("barState", () => {
  it("has no colour without checks", () => expect(barState(0, 0, 0)).toBe("none"));
  it("is green when every check passed", () => expect(barState(720, 10, 0)).toBe("up"));
  it("is yellow on a single failure", () => expect(barState(720, 0, 1)).toBe("degraded"));
  it("is yellow when 5 % of checks were slow", () => expect(barState(720, 36, 0)).toBe("degraded"));
  it("is red from 1 % failed", () => expect(barState(720, 0, 8)).toBe("down"));
});

describe("compareVersions", () => {
  it("sorts by number, not by text", () => expect(["v1.9.0", "v1.10.2", "v1.1.9"].sort(compareVersions)).toEqual(["v1.1.9", "v1.9.0", "v1.10.2"]));
  it("puts odd tags below releases", () => expect(compareVersions("nightly", "v0.0.1")).toBeLessThan(0));
});

describe("isDailyTick", () => {
  it("fires on 03:00 UTC", () => expect(isDailyTick(Date.parse("2026-10-16T03:00:00Z"))).toBe(true));
  it("does not fire on 03:02 UTC", () => expect(isDailyTick(Date.parse("2026-10-16T03:02:00Z"))).toBe(false));
});

describe("alertText", () => {
  const target: Target = { id: "api", name: "API", url: "https://api.test/health", kind: "health" };
  const r = (state: "up" | "degraded" | "down") => ({ target: "api", state, latencyMs: state === "down" ? null : 120, version: null, detail: state === "down" ? "HTTP 503" : null });
  it("stays quiet on the first check", () => expect(alertText(target, r("down"), undefined)).toBeNull());
  it("alerts when a target falls", () => expect(alertText(target, r("down"), "up")).toContain("HTTP 503"));
  it("alerts when it comes back", () => expect(alertText(target, r("up"), "down")).toContain("знову відповідає"));
  it("stays quiet on slow-and-back", () => expect(alertText(target, r("up"), "degraded")).toBeNull());
});

describe("parsePing", () => {
  it("accepts a ping and dedupes krais", () => {
    const result = parsePing(ping({ krais: ["ua", "cz", "cz"] }));
    expect(result).toMatchObject({ ping: { krais: ["cz", "ua"], members: 130 } });
  });
  it("lowercases the instance id", () => {
    expect(parsePing(ping({ instanceId: "3F2A9C1E-5B7D-4E8A-9C21-7D4E5F6A8B90" }))).toMatchObject({ ping: { instanceId: "3f2a9c1e-5b7d-4e8a-9c21-7d4e5f6a8b90" } });
  });
  it.each([
    [{ instanceId: "not-a-uuid" }, "instanceId"],
    [{ version: "v1 <script>" }, "version"],
    [{ kind: "Prod Server" }, "kind"],
    [{ krais: ["CZ!"] }, "krais"],
    [{ members: -1 }, "members"],
    [{ members: 1.5 }, "members"],
  ])("refuses %o", (overrides, field) => {
    const result = parsePing(ping(overrides));
    expect("error" in result && result.error.startsWith(field)).toBe(true);
  });
  it("treats missing counts as zero", () => {
    expect(parsePing({ instanceId: ping().instanceId, version: "v1.2.0", kind: "selfhost" })).toMatchObject({ ping: { kurins: 0, requests24h: 0, krais: [] } });
  });
});
