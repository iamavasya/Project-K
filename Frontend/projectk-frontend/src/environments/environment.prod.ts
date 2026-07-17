const runtimeConfig = (globalThis as { __PROJECTK_CONFIG__?: { apiUrl?: string } }).__PROJECTK_CONFIG__;

export const environment = {
  production: true,
  apiUrl: runtimeConfig?.apiUrl || 'https://api-projectk.rostyslav-mukha.dev/api',
  version: 'v0.0.0-dev',
  codeName: 'LocalDevelopment',
  isF1TierBackend: false
};
