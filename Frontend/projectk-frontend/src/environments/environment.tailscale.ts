const runtimeConfig = (globalThis as { __PROJECTK_CONFIG__?: { apiUrl?: string; environmentName?: string; appName?: string } }).__PROJECTK_CONFIG__;

export const environment = {
  production: false,
  // The tailnet address is not committed: the tailscale compose passes it through env.js
  // (PROJECTK_API_URL in docker/env/tailscale.env). This is only what a bare `ng serve` gets.
  apiUrl: runtimeConfig?.apiUrl || 'http://localhost:5205/api',
  version: 'v0.0.0-dev',
  codeName: 'TailscaleDevelopment',
  envName: runtimeConfig?.environmentName || 'Development',
  appName: runtimeConfig?.appName || 'Лілейка',
  isF1TierBackend: false
};
