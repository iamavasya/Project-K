import { cloudflareTest, readD1Migrations } from "@cloudflare/vitest-plugin";
import { generateKeyPairSync } from "node:crypto";
import { defineConfig } from "vitest/config";

// A throwaway Access signing key: tests mint their own Cf-Access-Jwt-Assertion tokens.
const { publicKey, privateKey } = generateKeyPairSync("rsa", { modulusLength: 2048 });
const kid = "test-key";

export default defineConfig(async () => {
  const migrations = await readD1Migrations("./migrations");
  return {
    plugins: [
      cloudflareTest({
        main: "./src/index.ts",
        wrangler: { configPath: "./wrangler.jsonc" },
        miniflare: {
          bindings: {
            TEST_MIGRATIONS: migrations,
            TARGETS: [
              { id: "api", name: "API", url: "https://api.test/health", kind: "health" },
              { id: "web", name: "Веб", url: "https://web.test/", kind: "http" },
            ],
            ACCESS_TEAM_DOMAIN: "https://test.cloudflareaccess.com",
            ACCESS_AUD: "test-aud",
            OWNER_EMAILS: "owner@example.com",
            ACCESS_JWKS: JSON.stringify({ keys: [{ ...publicKey.export({ format: "jwk" }), kid, alg: "RS256" }] }),
            TEST_PRIVATE_JWK: JSON.stringify({ ...privateKey.export({ format: "jwk" }), kid }),
            STATUS_CACHE_SECONDS: "0",
            TELEGRAM_BOT_TOKEN: "test-token",
            TELEGRAM_CHAT_ID: "42",
          },
        },
      }),
    ],
    test: { setupFiles: ["./test/setup.ts"] },
  };
});
