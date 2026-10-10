import { expect, test } from '@playwright/test';

/**
 * The component sheet at /ui, shot whole in light and dark: iOS on the iPhone project and Material on
 * the Android one. These screenshots are the quick reference for the Лілейка theme
 * (test-results/pwa-screens/<project>-ui-<scheme>.png).
 */
for (const scheme of ['light', 'dark'] as const) {
  test(`shows the component sheet, Ionic next to Лілейка (${scheme})`, async ({ page }, info) => {
    const mode = info.project.name === 'iphone' ? 'ios' : 'md';
    await page.emulateMedia({ colorScheme: scheme });
    await page.goto(`./ui?ionic:mode=${mode}`);

    await expect(page.locator('html')).toHaveAttribute('mode', mode);
    await expect(page.getByTestId(`mode-${mode}`)).toHaveClass(/current/);
    const sections = ['buttons', 'tabs', 'switches', 'inputs', 'list', 'tags', 'card', 'bars', 'glass'];
    await Promise.all(sections.map((section) => expect(page.getByTestId(section)).toBeVisible()));
    // The brand side is set in Manrope; the Ionic side keeps the platform font.
    await page.evaluate(() => document.fonts.ready);
    const brandFont = await page.locator('.lk ion-button').first().evaluate((el) => getComputedStyle(el).fontFamily);
    expect(brandFont).toContain('Manrope');

    // Grow the window to the whole sheet so one screenshot holds it.
    const viewport = page.viewportSize()!;
    const sheet = await page.locator('.sheet').boundingBox();
    await page.setViewportSize({ width: viewport.width, height: Math.ceil((sheet?.y ?? 0) + (sheet?.height ?? 0)) });
    await page.waitForTimeout(300);
    await page.screenshot({ path: `test-results/pwa-screens/${info.project.name}-ui-${scheme}.png`, scale: 'css' });
  });
}
