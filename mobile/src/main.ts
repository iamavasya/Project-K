import { bootstrapApplication } from '@angular/platform-browser';
import { Capacitor } from '@capacitor/core';
import { appConfig } from './app/app.config';
import { App } from './app/app';
import { loadRuntimeConfig } from './app/runtime-config';

const ready = Capacitor.isNativePlatform() ? Promise.resolve() : loadRuntimeConfig();

ready
  .then(() => bootstrapApplication(App, appConfig))
  .catch((err) => console.error(err));
