/**
 * How Лілейка gets onto this device's home screen, read from the user agent and a few flags. Pure,
 * so every browser the install page knows about is a unit test, not a phone on the desk.
 *
 * - `installed` — already running from the home screen (or the dock);
 * - `in-app` — a messenger's or social app's own browser (Telegram, Instagram…): none of them can
 *   install a web app, so the person has to open the page in Safari or Chrome first;
 * - `ios` — iPhone/iPad: Safari, and since iOS 16.4 Chrome, Edge and Firefox too, add to the home
 *   screen from their Share sheet; there is no install prompt to call;
 * - `android` — Chromium browsers there fire `beforeinstallprompt` (one tap), the rest have a menu item;
 * - `desktop` — Chrome/Edge install from the address bar, Safari on macOS 14+ «Додати в Dock».
 */
export type InstallGuide =
  | { kind: 'installed' }
  | { kind: 'in-app'; os: 'ios' | 'android'; app: string | null }
  | {
      kind: 'ios';
      device: 'iPhone' | 'iPad';
      browser: IosBrowser;
      safari26: boolean;
      tooOld: boolean;
    }
  | { kind: 'android'; browser: AndroidBrowser }
  | { kind: 'desktop'; browser: DesktopBrowser; mac: boolean };

export type IosBrowser = 'safari' | 'chrome' | 'edge' | 'firefox' | 'other';
export type AndroidBrowser = 'chrome' | 'samsung' | 'edge' | 'firefox' | 'opera' | 'other';
export type DesktopBrowser = 'chrome' | 'edge' | 'safari' | 'firefox' | 'other';

export interface InstallFacts {
  userAgent: string;
  /** display-mode: standalone, or iOS's navigator.standalone. */
  standalone: boolean;
  /** iPadOS asks for the desktop site and says «Macintosh»; only the touch points give it away. */
  maxTouchPoints: number;
}

/** Apps whose own browser opens links; matched in order (Messenger before Facebook). */
const IN_APP: [RegExp, string][] = [
  [/Telegram/i, 'Telegram'],
  [/Instagram/, 'Instagram'],
  [/FBAN\/Messenger|MessengerForiOS|FB_IAB\/MESSENGER|Orca-Android/, 'Messenger'],
  [/FBAN|FBAV|FB_IAB|FBIOS/, 'Facebook'],
  [/Barcelona/, 'Threads'],
  [/musical_ly|TikTok|BytedanceWebview|trill_/i, 'TikTok'],
  [/Viber/i, 'Viber'],
  [/WhatsApp/i, 'WhatsApp'],
  [/LinkedInApp/, 'LinkedIn'],
  [/Snapchat/, 'Snapchat'],
  [/Twitter/i, 'X'],
  [/\bLine\//, 'LINE'],
  [/Discord/i, 'Discord'],
  [/\bGSA\//, 'Google'],
];

export function installGuide(facts: InstallFacts): InstallGuide {
  const ua = facts.userAgent;
  if (facts.standalone) return { kind: 'installed' };

  const iPad = /iPad/.test(ua) || (/Macintosh/.test(ua) && facts.maxTouchPoints > 1);
  const ios = iPad || /iPhone|iPod/.test(ua);
  const android = /Android/.test(ua);

  if (ios || android) {
    const app = IN_APP.find(([pattern]) => pattern.test(ua))?.[1] ?? null;
    // Android's WebView marks itself «; wv)»; iOS's WKWebView leaves out the «Safari/» token that
    // Safari and every real browser on iOS send.
    const webview = android ? /; wv\)/.test(ua) : !/Safari\//.test(ua);
    if (app || webview) return { kind: 'in-app', os: ios ? 'ios' : 'android', app };
  }

  if (ios) {
    const browser: IosBrowser = /CriOS\//.test(ua)
      ? 'chrome'
      : /EdgiOS\//.test(ua)
        ? 'edge'
        : /FxiOS\//.test(ua)
          ? 'firefox'
          : /OPiOS|OPT\/|YaBrowser|DuckDuckGo|Brave/.test(ua)
            ? 'other'
            : 'safari';
    // Safari 26 freezes the iOS number in the user agent (at 18_6), so the Safari version tells.
    const safariVersion = Number(/Version\/(\d+)/.exec(ua)?.[1] ?? 0);
    return {
      kind: 'ios',
      device: iPad ? 'iPad' : 'iPhone',
      browser,
      safari26: browser === 'safari' && safariVersion >= 26,
      // Before 16.4 only Safari could add to the home screen.
      tooOld: browser !== 'safari' && iosBefore(ua, 16, 4),
    };
  }

  if (android) {
    const browser: AndroidBrowser = /SamsungBrowser\//.test(ua)
      ? 'samsung'
      : /EdgA\//.test(ua)
        ? 'edge'
        : /Firefox\//.test(ua)
          ? 'firefox'
          : /OPR\/|Opera/.test(ua)
            ? 'opera'
            : /Chrome\//.test(ua) && !/YaBrowser|MiuiBrowser|HuaweiBrowser|UCBrowser/.test(ua)
              ? 'chrome'
              : 'other';
    return { kind: 'android', browser };
  }

  const browser: DesktopBrowser = /Edg\//.test(ua)
    ? 'edge'
    : /Firefox\//.test(ua)
      ? 'firefox'
      : /Chrome\//.test(ua)
        ? 'chrome'
        : /Safari\//.test(ua)
          ? 'safari'
          : 'other';
  return { kind: 'desktop', browser, mac: /Macintosh/.test(ua) };
}

/** Where the browser's own install dialog can come (beforeinstallprompt): Chromium, not iOS. */
export function canOfferPrompt(guide: InstallGuide): boolean {
  if (guide.kind === 'android') return guide.browser !== 'firefox';
  if (guide.kind === 'desktop') return guide.browser === 'chrome' || guide.browser === 'edge';
  return false;
}

function iosBefore(ua: string, major: number, minor: number): boolean {
  const match = /OS (\d+)_(\d+)/.exec(ua);
  if (!match) return false;
  const [have, haveMinor] = [Number(match[1]), Number(match[2])];
  return have < major || (have === major && haveMinor < minor);
}

/** The facts of this browser tab. */
export function currentInstallFacts(): InstallFacts {
  const nav = globalThis.navigator as (Navigator & { standalone?: boolean }) | undefined;
  return {
    userAgent: nav?.userAgent ?? '',
    standalone:
      nav?.standalone === true ||
      globalThis.matchMedia?.('(display-mode: standalone)').matches === true,
    maxTouchPoints: nav?.maxTouchPoints ?? 0,
  };
}

/**
 * Text with the system's own labels in «…» comes out as runs, the quoted ones marked `strong`, so a
 * step reads «Натисни **«Поділитися»**» without HTML in the strings.
 */
export function emphasize(text: string): { text: string; strong: boolean }[] {
  return text
    .split(/(«[^»]*»)/)
    .filter((part) => part.length > 0)
    .map((part) => ({ text: part, strong: part.startsWith('«') }));
}
