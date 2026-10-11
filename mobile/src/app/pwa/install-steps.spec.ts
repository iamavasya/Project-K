import { manualInstall } from './install-steps';

describe('manualInstall', () => {
  it('walks iPhone Safari through Share, «На початковий екран» and «Додати», with pictures', () => {
    const steps = manualInstall({
      kind: 'ios',
      device: 'iPhone',
      browser: 'safari',
      safari26: false,
      tooOld: false,
    });
    expect(steps.steps.map((s) => s.art)).toEqual(['ios-share', 'ios-sheet', 'ios-add']);
    // The home-screen app keeps its own storage on iOS: one more sign-in there.
    expect(steps.notes.some((n) => n.includes('увійди ще раз'))).toBe(true);
  });

  it('starts Safari 26 from «⋯»', () => {
    const steps = manualInstall({
      kind: 'ios',
      device: 'iPhone',
      browser: 'safari',
      safari26: true,
      tooOld: false,
    });
    expect(steps.steps[0]).toEqual(expect.objectContaining({ art: 'ios-more' }));
  });

  it('sends an old iOS out of Chrome to Safari with the link', () => {
    const steps = manualInstall({
      kind: 'ios',
      device: 'iPhone',
      browser: 'chrome',
      safari26: false,
      tooOld: true,
    });
    expect(steps.title).toBe('Відкрий у Safari');
    expect(steps.copyLink).toBe(true);
  });

  it('gets out of an app’s own browser: Safari on iOS, Chrome (with a one-tap link) on Android', () => {
    expect(manualInstall({ kind: 'in-app', os: 'ios', app: 'Instagram' })).toEqual(
      expect.objectContaining({ title: 'Відкрий у Safari', copyLink: true, openInChrome: false }),
    );
    const android = manualInstall({ kind: 'in-app', os: 'android', app: 'Telegram' });
    expect(android.title).toBe('Відкрий у Chrome');
    expect(android.intro).toContain('Telegram');
    expect(android.openInChrome).toBe(true);
  });

  it('points a desktop Firefox at Chrome or Edge', () => {
    expect(manualInstall({ kind: 'desktop', browser: 'firefox', mac: false }).copyLink).toBe(true);
    expect(manualInstall({ kind: 'desktop', browser: 'safari', mac: true }).steps[0].art).toBe(
      'macos-dock',
    );
  });
});
