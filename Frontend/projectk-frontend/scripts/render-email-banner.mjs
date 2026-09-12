// Renders public/assets/lileyka-banner-1080x288.svg to a PNG for the emails: mail clients do not
// render SVG, and the letter frame is 600px wide, so the PNG is 1200px for retina screens.
// Run from Frontend/projectk-frontend: node scripts/render-email-banner.mjs
import { chromium } from 'playwright';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

const svg = readFileSync(resolve('public/assets/lileyka-banner-1080x288.svg'), 'utf8');
const out = resolve('public/assets/images/email-banner.png');

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1200, height: 320 }, deviceScaleFactor: 1 });
await page.setContent(`<!doctype html><html><body style="margin:0;background:#0E6E4E">
  <div style="width:1200px;height:320px;display:flex;align-items:center;justify-content:center;overflow:hidden">
    ${svg.replace('<svg ', '<svg style="width:1200px;height:320px" preserveAspectRatio="xMidYMid slice" ')}
  </div></body></html>`);
await page.screenshot({ path: out, clip: { x: 0, y: 0, width: 1200, height: 320 } });
await browser.close();
console.log('written', out);
