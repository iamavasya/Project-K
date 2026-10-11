import { defineConfig, devices } from '@playwright/test';

// PWA smoke: the production build, served under /m/ like the web image does, as iPhone Safari (WebKit) and Android Chrome (Chromium).
// Screenshots land in test-results/pwa-screens. Locally, PW_CHROMIUM=/path/to/chrome reuses an
// installed Chromium instead of downloading one.
const chromium = process.env['PW_CHROMIUM'];
// PW_BASE_URL points the run at an already running server (e.g. the web image's nginx) instead.
const external = process.env['PW_BASE_URL'];

export default defineConfig({
  testDir: 'e2e',
  testMatch: '*.pw.ts',
  outputDir: 'test-results/artifacts',
  reporter: [['list']],
  use: {
    baseURL: external ?? 'http://127.0.0.1:4300/m/',
    locale: 'uk-UA',
    // The service worker proxies API calls, and WebKit cannot route requests made by a worker, so
    // mocked API calls would miss. Only the offline test turns it on (Chromium).
    serviceWorkers: 'block',
  },
  webServer: external
    ? undefined
    : {
        command: 'node scripts/serve-www.mjs',
        url: 'http://127.0.0.1:4300/m/',
        reuseExistingServer: !process.env['CI'],
      },
  projects: [
    // WebKit runs the long journeys (a member's card, a leader's dues) at about twice Chromium's time.
    { name: 'iphone', timeout: 60_000, use: { ...devices['iPhone 15'] } },
    {
      name: 'android',
      use: {
        ...devices['Pixel 7'],
        ...(chromium ? { launchOptions: { executablePath: chromium } } : {}),
      },
    },
  ],
});
