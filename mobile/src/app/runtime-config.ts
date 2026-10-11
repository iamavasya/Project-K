import { environment } from '../environments/environment';

/** Same runtime config the web app reads: nginx writes it to /env.js from the container's env. */
interface ProjectkConfig {
  apiUrl?: string;
  environmentName?: string;
}

declare global {
  // eslint-disable-next-line no-var
  var __PROJECTK_CONFIG__: ProjectkConfig | undefined;
}

/**
 * The browser build lives under /m/ next to the web app, so it loads the web's /env.js before
 * starting: a self-hosted server then points the PWA at its own API with no rebuild. The native
 * shells and `ng serve` have no env.js and keep the build-time values.
 */
export function loadRuntimeConfig(): Promise<void> {
  return new Promise((resolve) => {
    const script = document.createElement('script');
    script.src = '/env.js';
    script.onload = () => resolve();
    script.onerror = () => resolve();
    document.head.appendChild(script);
  });
}

export function apiUrl(): string {
  const fromEnv = globalThis.__PROJECTK_CONFIG__?.apiUrl;
  // An unsubstituted template (`${PROJECTK_API_URL}`) means env.js was never rendered.
  return fromEnv && !fromEnv.includes('${') ? fromEnv : environment.apiUrl;
}
