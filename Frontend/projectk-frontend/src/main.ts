import { registerLocaleData } from '@angular/common';
import localeUk from '@angular/common/locales/uk';
import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';
import { captureInstallPrompt } from './app/features/systemModule/services/install-app-service/install-app.service';
import { redirectPhoneToApp } from './app/features/systemModule/functions/mobile-redirect.function';
import { acceptDatePickerInputWithoutKeydown } from './app/shared/functions/date-picker-input.function';
import { environment } from './environments/environment';

// LocalUtcDatePipe formats with the 'uk-UA' locale; without its data registered, formatDate throws
// NG0701 mid-render and aborts change detection (e.g. only the first notification row painted).
// Register under both ids so 'uk' and 'uk-UA' resolve.
registerLocaleData(localeUk, 'uk');
registerLocaleData(localeUk, 'uk-UA');

// A phone goes to the phone app (/m/) before anything renders, unless it chose «Повна версія».
// The demo on the docs site has no /m/ next to it, so it stays put.
if (environment.isStaticDemo || !redirectPhoneToApp()) {
  // Before Angular starts: Chrome may offer the phone app's install right away (see InstallAppService).
  captureInstallPrompt();
  acceptDatePickerInputWithoutKeydown();

  bootstrapApplication(App, appConfig)
    .catch((err) => console.error(err));
}
