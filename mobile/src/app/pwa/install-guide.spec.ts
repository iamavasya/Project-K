import { InstallFacts, canOfferPrompt, emphasize, installGuide } from './install-guide';

// User agents as the browsers send them (versions trimmed where they do not matter).
const UA = {
  iphoneSafari18:
    'Mozilla/5.0 (iPhone; CPU iPhone OS 18_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Mobile/15E148 Safari/604.1',
  iphoneSafari26:
    'Mozilla/5.0 (iPhone; CPU iPhone OS 18_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/26.0 Mobile/15E148 Safari/604.1',
  iphoneChrome:
    'Mozilla/5.0 (iPhone; CPU iPhone OS 17_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) CriOS/126.0.6478.54 Mobile/15E148 Safari/604.1',
  iphoneChromeOld:
    'Mozilla/5.0 (iPhone; CPU iPhone OS 16_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) CriOS/110.0.5481.83 Mobile/15E148 Safari/604.1',
  iphoneEdge:
    'Mozilla/5.0 (iPhone; CPU iPhone OS 17_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 EdgiOS/126.2592.56 Mobile/15E148 Safari/605.1.15',
  iphoneFirefox:
    'Mozilla/5.0 (iPhone; CPU iPhone OS 17_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) FxiOS/127.0 Mobile/15E148 Safari/605.1.15',
  ipadDesktopSite:
    'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.5 Safari/605.1.15',
  iphoneInstagram:
    'Mozilla/5.0 (iPhone; CPU iPhone OS 17_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148 Instagram 337.0.3.23.54 (iPhone14,5; iOS 17_5; uk_UA; uk; scale=3.00; 1170x2532; 614410279)',
  iphoneFacebook:
    'Mozilla/5.0 (iPhone; CPU iPhone OS 17_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148 [FBAN/FBIOS;FBAV/470.0.0.38.109;FBBV/612540207;FBDV/iPhone14,5;FBMD/iPhone;FBSN/iOS;FBSV/17.5;FBSS/3;FBCR/;FBID/phone;FBLC/uk_UA;FBOP/80]',
  iphoneWebView:
    'Mozilla/5.0 (iPhone; CPU iPhone OS 17_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148',
  androidChrome:
    'Mozilla/5.0 (Linux; Android 10; K) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Mobile Safari/537.36',
  androidSamsung:
    'Mozilla/5.0 (Linux; Android 14; SM-S911B) AppleWebKit/537.36 (KHTML, like Gecko) SamsungBrowser/25.0 Chrome/121.0.0.0 Mobile Safari/537.36',
  androidEdge:
    'Mozilla/5.0 (Linux; Android 10; K) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Mobile Safari/537.36 EdgA/126.0.2592.80',
  androidFirefox: 'Mozilla/5.0 (Android 14; Mobile; rv:127.0) Gecko/127.0 Firefox/127.0',
  androidTelegram:
    'Mozilla/5.0 (Linux; Android 14; Pixel 7 Build/AP2A.240705.005; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/126.0.6478.71 Mobile Safari/537.36 Telegram-Android/11.2.3 (Google Pixel 7; Android 14; SDK 34; HIGH)',
  androidTikTok:
    'Mozilla/5.0 (Linux; Android 13; SM-A536B Build/TP1A.220624.014; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/125.0.6422.165 Mobile Safari/537.36 trill_350003 JsSdk/1.0 NetType/WIFI Channel/googleplay AppName/musical_ly app_version/35.0.3',
  androidViber:
    'Mozilla/5.0 (Linux; Android 13; SM-A536B; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/125.0.6422.165 Mobile Safari/537.36 Viber/22.8.0.0',
  androidWebView:
    'Mozilla/5.0 (Linux; Android 13; SM-A536B; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/125.0.6422.165 Mobile Safari/537.36',
  windowsChrome:
    'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36',
  windowsEdge:
    'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36 Edg/126.0.2592.68',
  macSafari:
    'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.5 Safari/605.1.15',
  linuxFirefox: 'Mozilla/5.0 (X11; Linux x86_64; rv:127.0) Gecko/20100101 Firefox/127.0',
};

const facts = (userAgent: string, over: Partial<InstallFacts> = {}): InstallFacts => ({
  userAgent,
  standalone: false,
  maxTouchPoints: /iPhone|Android/.test(userAgent) ? 5 : 0,
  ...over,
});

describe('installGuide', () => {
  it('knows when it already runs from the home screen', () => {
    expect(installGuide(facts(UA.iphoneSafari18, { standalone: true }))).toEqual({
      kind: 'installed',
    });
    // A standalone iOS app sends no «Safari/» token: that is not an in-app browser.
    expect(installGuide(facts(UA.iphoneWebView, { standalone: true }))).toEqual({
      kind: 'installed',
    });
    expect(installGuide(facts(UA.androidChrome, { standalone: true }))).toEqual({
      kind: 'installed',
    });
  });

  it('walks iPhone Safari through the Share sheet, behind «⋯» on Safari 26', () => {
    expect(installGuide(facts(UA.iphoneSafari18))).toEqual({
      kind: 'ios',
      device: 'iPhone',
      browser: 'safari',
      safari26: false,
      tooOld: false,
    });
    expect(installGuide(facts(UA.iphoneSafari26))).toMatchObject({
      kind: 'ios',
      browser: 'safari',
      safari26: true,
    });
  });

  it('tells the other iOS browsers apart, and those too old to add to the home screen', () => {
    expect(installGuide(facts(UA.iphoneChrome))).toMatchObject({
      kind: 'ios',
      browser: 'chrome',
      tooOld: false,
    });
    expect(installGuide(facts(UA.iphoneChromeOld))).toMatchObject({
      kind: 'ios',
      browser: 'chrome',
      tooOld: true,
    });
    expect(installGuide(facts(UA.iphoneEdge))).toMatchObject({ kind: 'ios', browser: 'edge' });
    expect(installGuide(facts(UA.iphoneFirefox))).toMatchObject({
      kind: 'ios',
      browser: 'firefox',
    });
  });

  it('sees an iPad behind the desktop site it asks for, and a Mac as a Mac', () => {
    expect(installGuide(facts(UA.ipadDesktopSite, { maxTouchPoints: 5 }))).toMatchObject({
      kind: 'ios',
      device: 'iPad',
      browser: 'safari',
    });
    expect(installGuide(facts(UA.macSafari))).toEqual({
      kind: 'desktop',
      browser: 'safari',
      mac: true,
    });
  });

  it('names the app whose own browser opened the link', () => {
    expect(installGuide(facts(UA.iphoneInstagram))).toEqual({
      kind: 'in-app',
      os: 'ios',
      app: 'Instagram',
    });
    expect(installGuide(facts(UA.iphoneFacebook))).toEqual({
      kind: 'in-app',
      os: 'ios',
      app: 'Facebook',
    });
    expect(installGuide(facts(UA.androidTelegram))).toEqual({
      kind: 'in-app',
      os: 'android',
      app: 'Telegram',
    });
    expect(installGuide(facts(UA.androidTikTok))).toEqual({
      kind: 'in-app',
      os: 'android',
      app: 'TikTok',
    });
    expect(installGuide(facts(UA.androidViber))).toEqual({
      kind: 'in-app',
      os: 'android',
      app: 'Viber',
    });
  });

  it('treats an unnamed web view as an in-app browser too', () => {
    expect(installGuide(facts(UA.iphoneWebView))).toEqual({ kind: 'in-app', os: 'ios', app: null });
    expect(installGuide(facts(UA.androidWebView))).toEqual({
      kind: 'in-app',
      os: 'android',
      app: null,
    });
  });

  it('tells the Android browsers apart', () => {
    expect(installGuide(facts(UA.androidChrome))).toEqual({ kind: 'android', browser: 'chrome' });
    expect(installGuide(facts(UA.androidSamsung))).toEqual({ kind: 'android', browser: 'samsung' });
    expect(installGuide(facts(UA.androidEdge))).toEqual({ kind: 'android', browser: 'edge' });
    expect(installGuide(facts(UA.androidFirefox))).toEqual({ kind: 'android', browser: 'firefox' });
  });

  it('tells the desktop browsers apart', () => {
    expect(installGuide(facts(UA.windowsChrome))).toEqual({
      kind: 'desktop',
      browser: 'chrome',
      mac: false,
    });
    expect(installGuide(facts(UA.windowsEdge))).toEqual({
      kind: 'desktop',
      browser: 'edge',
      mac: false,
    });
    expect(installGuide(facts(UA.linuxFirefox))).toEqual({
      kind: 'desktop',
      browser: 'firefox',
      mac: false,
    });
  });
});

describe('canOfferPrompt', () => {
  it('waits for the one-tap dialog only where Chromium can fire it', () => {
    expect(canOfferPrompt(installGuide(facts(UA.androidChrome)))).toBe(true);
    expect(canOfferPrompt(installGuide(facts(UA.androidSamsung)))).toBe(true);
    expect(canOfferPrompt(installGuide(facts(UA.windowsEdge)))).toBe(true);
    expect(canOfferPrompt(installGuide(facts(UA.androidFirefox)))).toBe(false);
    expect(canOfferPrompt(installGuide(facts(UA.iphoneChrome)))).toBe(false);
    expect(canOfferPrompt(installGuide(facts(UA.androidTelegram)))).toBe(false);
    expect(canOfferPrompt(installGuide(facts(UA.macSafari)))).toBe(false);
  });
});

describe('emphasize', () => {
  it('marks the quoted system labels', () => {
    expect(emphasize('Натисни «Поділитися», потім «Додати».')).toEqual([
      { text: 'Натисни ', strong: false },
      { text: '«Поділитися»', strong: true },
      { text: ', потім ', strong: false },
      { text: '«Додати»', strong: true },
      { text: '.', strong: false },
    ]);
  });
});
