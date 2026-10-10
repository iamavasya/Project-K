import { defineConfig, devices } from '@playwright/test';

// PWA smoke: the production build as iPhone Safari (WebKit) and Android Chrome (Chromium).
// Screenshots land in test-results/pwa-screens. Locally, PW_CHROMIUM=/path/to/chrome reuses an
// installed Chromium instead of downloading one.
const chromium = process.env['PW_CHROMIUM'];

export default defineConfig({
  testDir: 'e2e',
  testMatch: '*.pw.ts',
  outputDir: 'test-results/artifacts',
  reporter: [['list']],
  use: { baseURL: 'http://127.0.0.1:4300', locale: 'uk-UA' },
  webServer: {
    command: 'node scripts/serve-www.mjs',
    url: 'http://127.0.0.1:4300',
    reuseExistingServer: !process.env['CI'],
  },
  projects: [
    { name: 'iphone', use: { ...devices['iPhone 15'] } },
    {
      name: 'android',
      use: {
        ...devices['Pixel 7'],
        ...(chromium ? { launchOptions: { executablePath: chromium } } : {}),
      },
    },
  ],
});
