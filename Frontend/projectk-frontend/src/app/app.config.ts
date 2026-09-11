import { ApplicationConfig, inject, provideAppInitializer, provideBrowserGlobalErrorListeners, provideZoneChangeDetection } from '@angular/core';
import { NavigationEnd, Router, TitleStrategy, provideRouter, withNavigationErrorHandler } from '@angular/router';

import { routes } from './app.routes';
import { clearStaleAppShellMarker, recoverFromStaleAppShell } from './features/systemModule/functions/stale-app-shell.function';
import { ProjectKTitleStrategy } from './features/systemModule/services/page-title.strategy';
import { provideOptimus } from '@openng/optimus-ui/config';
import { LileykaPreset } from './lileyka-preset';
import { HTTP_INTERCEPTORS, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { AuthInterceptor } from './features/authModule/services/auth.interceptor';
import { HealthInterceptor } from './features/systemModule/services/health.interceptor';
import { HealthBannerService } from './features/systemModule/services/health-banner-service/health-banner.service';
import { ThemeService } from './features/systemModule/services/theme-service/theme.service';
import { MessageService } from '@openng/optimus-ui/api';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    // Кожен маршрут тут лінивий, тож після деплою перший же перехід у вже відкритій вкладці просить
    // чанк, якого на сервері більше немає, і клік просто нічого не робить. Ловимо це й ставимо
    // вкладку на свіжу оболонку — один раз, щоб зламаний деплой не перетворився на цикл.
    provideRouter(routes, withNavigationErrorHandler(error => {
      recoverFromStaleAppShell(error);
    })),
    { provide: TitleStrategy, useClass: ProjectKTitleStrategy },
    MessageService,
    provideOptimus({
        translation: {
          firstDayOfWeek: 1,
          dayNames: ['Неділя', 'Понеділок', 'Вівторок', 'Середа', 'Четвер', 'Пʼятниця', 'Субота'],
          dayNamesShort: ['Нед', 'Пон', 'Вів', 'Сер', 'Чет', 'Птн', 'Суб'],
          dayNamesMin: ['Нд', 'Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб'],
          monthNames: ['Січень', 'Лютий', 'Березень', 'Квітень', 'Травень', 'Червень', 'Липень', 'Серпень', 'Вересень', 'Жовтень', 'Листопад', 'Грудень'],
          monthNamesShort: ['Січ', 'Лют', 'Бер', 'Кві', 'Тра', 'Чер', 'Лип', 'Сер', 'Вер', 'Жов', 'Лис', 'Гру'],
          today: 'Сьогодні',
          clear: 'Очистити',
          accept: 'Так',
          reject: 'Ні'
        },
        theme: {
          preset: LileykaPreset,
          options: {
            darkModeSelector: '[data-theme="dark"]',
          }
        }
    }),
    provideHttpClient(withInterceptorsFromDi()),
    // Constructing ThemeService applies the stored preference. Without this it would only
    // happen once the toolbar renders, so login and welcome would ignore a dark preference.
    provideAppInitializer(() => void inject(ThemeService)),
    // Вдала навігація означає, що оболонка жива — знімаємо позначку, щоб наступний деплой знову
    // мав право на одну спробу відновитись.
    provideAppInitializer(() => {
      inject(Router).events.subscribe(event => {
        if (event instanceof NavigationEnd) {
          clearStaleAppShellMarker();
        }
      });
    }),
    provideAppInitializer(() => inject(HealthBannerService).startSessionCheck()),
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: HealthInterceptor, multi: true }
  ]
};
