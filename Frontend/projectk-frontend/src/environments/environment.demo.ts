// The static demo: the same build as production, but every API call is answered from recorded
// fixtures by DemoApiInterceptor. There is no server behind it, so `apiUrl` is a marker the
// interceptor recognises, never an address that is fetched.
export const environment = {
  production: true,
  apiUrl: 'https://demo.invalid/api',
  version: 'v1.0.0-demo',
  codeName: 'Demo',
  envName: 'Demo',
  appName: 'Лілейка',
  isF1TierBackend: false,
  isStaticDemo: true
};
