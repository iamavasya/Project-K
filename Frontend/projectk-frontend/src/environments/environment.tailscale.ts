const runtimeConfig = (globalThis as { __PROJECTK_CONFIG__?: { apiUrl?: string; environmentName?: string } }).__PROJECTK_CONFIG__;

export const environment = {
  production: false,
  apiUrl: runtimeConfig?.apiUrl || 'http://100.64.66.7:5205/api',
  version: 'v0.0.0-dev',
  codeName: 'TailscaleDevelopment',
  envName: runtimeConfig?.environmentName || 'Development',
  isF1TierBackend: false
};
