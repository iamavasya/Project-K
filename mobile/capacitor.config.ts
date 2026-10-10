import type { CapacitorConfig } from '@capacitor/cli';

// Optional live-reload target for launches from Android Studio, e.g.
//   LIVE_RELOAD_URL=http://10.0.2.2:4200 npx cap sync android
// `npx cap run android -l --host 10.0.2.2 --port 4200` does the same on its own
// and reverts it on Ctrl+C, so prefer that. Never commit a synced config with a URL.
const liveReloadUrl = process.env['LIVE_RELOAD_URL'];

const config: CapacitorConfig = {
  appId: 'dev.rostyslavmukha.lileyka',
  appName: 'Лілейка',
  webDir: 'www',
  server: liveReloadUrl ? { url: liveReloadUrl, cleartext: true } : undefined,
  ios: {
    // @rdlabo/ionic-theme-ios27 also ships a native plugin (Native UI Shell, a preview) that needs
    // a newer SDK than Xcode 26.3 has; the app only uses its CSS and JS, so leave it out of the shell.
    includePlugins: ['@capacitor/app', '@capacitor/haptics', '@capacitor/keyboard', '@capacitor/status-bar'],
  },
};

export default config;
