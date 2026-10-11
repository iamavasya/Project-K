import { defineConfig, devices } from '@playwright/test';

const baseURL = process.env.PLAYWRIGHT_BASE_URL ?? 'http://localhost:4200';
const startFrontend = process.env.PLAYWRIGHT_START_FRONTEND !== 'false';

export default defineConfig({
  testDir: './e2e',
  timeout: 30_000,
  expect: {
    timeout: 7_500
  },
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: process.env.CI ? [['html', { open: 'never' }], ['github']] : [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL,
    extraHTTPHeaders: { 'X-RateLimit-Bypass': 'e2e-bypass-key' },
    // retain-on-failure, not on-first-retry: retries are 0 locally, so a first-retry trace is only
    // ever produced in CI. Every local flake so far had to be diagnosed without one.
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure'
  },
  webServer: startFrontend
    ? {
        command: 'npm run start -- --host localhost --port 4200',
        url: baseURL,
        reuseExistingServer: !process.env.CI,
        timeout: 120_000
      }
    : undefined,
  projects: [
    {
      name: 'auth-setup',
      testMatch: /auth\.setup\.ts/
    },
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
      dependencies: ['auth-setup']
    },
    {
      name: 'mobile-chrome',
      use: {
        ...devices['Pixel 7'],
        // A phone is sent to the phone app (/m/) unless it chose «Повна версія»; this project tests
        // the web itself on a phone. Role suites bring their own state, saved with the same flag.
        storageState: {
          cookies: [],
          origins: [{ origin: new URL(baseURL).origin, localStorage: [{ name: 'lk.preferWeb', value: '1' }] }]
        }
      },
      dependencies: ['auth-setup']
    }
  ]
});
