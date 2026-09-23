const runtimeConfig = (globalThis as { __PROJECTK_CONFIG__?: { apiUrl?: string; environmentName?: string; appName?: string; docsUrl?: string } }).__PROJECTK_CONFIG__;

export const environment = {
  production: true,
  apiUrl: runtimeConfig?.apiUrl || 'https://api-projectk.rostyslav-mukha.dev/api',
  version: 'v0.0.0-dev',
  codeName: 'LocalDevelopment',
  envName: runtimeConfig?.environmentName || 'Production',
  appName: runtimeConfig?.appName || 'Лілейка',
  docsUrl: runtimeConfig?.docsUrl || 'https://projectk-docs-and-demo.pages.dev/',
  isF1TierBackend: false,
  isStaticDemo: false
};
