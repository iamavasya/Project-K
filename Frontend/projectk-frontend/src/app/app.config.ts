import { ApplicationConfig, inject, provideAppInitializer, provideBrowserGlobalErrorListeners, provideZoneChangeDetection } from '@angular/core';
import { TitleStrategy, provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { ProjectKTitleStrategy } from './features/systemModule/services/page-title.strategy';
import { providePrimeNG } from 'primeng/config';
import { LilykaPreset } from './lilyka-preset';
import { HTTP_INTERCEPTORS, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { AuthInterceptor } from './features/authModule/services/auth.interceptor';
import { HealthInterceptor } from './features/systemModule/services/health.interceptor';
import { HealthBannerService } from './features/systemModule/services/health-banner.service';
import { MessageService } from 'primeng/api';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    { provide: TitleStrategy, useClass: ProjectKTitleStrategy },
    MessageService,
    providePrimeNG({
        translation: {
          firstDayOfWeek: 1,
          dayNames: ['Неділя', 'Понеділок', 'Вівторок', 'Середа', 'Четвер', 'Пʼятниця', 'Субота'],
          dayNamesShort: ['Нед', 'Пон', 'Вів', 'Сер', 'Чет', 'Птн', 'Суб'],
          dayNamesMin: ['Нд', 'Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб'],
          monthNames: ['Січень', 'Лютий', 'Березень', 'Квітень', 'Травень', 'Червень', 'Липень', 'Серпень', 'Вересень', 'Жовтень', 'Листопад', 'Грудень'],
          monthNamesShort: ['Січ', 'Лют', 'Бер', 'Кві', 'Тра', 'Чер', 'Лип', 'Сер', 'Вер', 'Жов', 'Лис', 'Гру'],
          today: 'Сьогодні',
          clear: 'Очистити'
        },
        theme: {
          preset: LilykaPreset,
          options: {
            darkModeSelector: '[data-theme="dark"]',
          }
        }
    }),
    provideHttpClient(withInterceptorsFromDi()),
    provideAppInitializer(() => inject(HealthBannerService).startSessionCheck()),
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: HealthInterceptor, multi: true }
  ]
};
