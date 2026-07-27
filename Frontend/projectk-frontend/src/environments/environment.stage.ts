const runtimeConfig = (globalThis as { __PROJECTK_CONFIG__?: { apiUrl?: string; environmentName?: string; appName?: string } }).__PROJECTK_CONFIG__;

export const environment = {
  production: true,
  apiUrl: runtimeConfig?.apiUrl || 'https://api-projectk-prod-b9dedsaucbcgf5fh.polandcentral-01.azurewebsites.net/api',
  version: 'v0.0.0-dev',
  codeName: 'LocalDevelopment',
  envName: runtimeConfig?.environmentName || 'Staging',
  appName: runtimeConfig?.appName || 'ProjectK',
  isF1TierBackend: true
};
