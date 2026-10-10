// Android emulator reaches the host machine at 10.0.2.2 (backend on :5205).
export const environment = {
  production: false,
  version: 'v0.0.0-dev',
  codename: 'LocalDevelopment',
  apiUrl: 'http://10.0.2.2:5205/api', // NOSONAR: local backend on the emulator host, dev builds only
};
