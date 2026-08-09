import { registerLocaleData } from '@angular/common';
import localeUk from '@angular/common/locales/uk';
import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';

// LocalUtcDatePipe formats with the 'uk-UA' locale; without its data registered, formatDate throws
// NG0701 mid-render and aborts change detection (e.g. only the first notification row painted).
// Register under both ids so 'uk' and 'uk-UA' resolve.
registerLocaleData(localeUk, 'uk');
registerLocaleData(localeUk, 'uk-UA');

bootstrapApplication(App, appConfig)
  .catch((err) => console.error(err));
