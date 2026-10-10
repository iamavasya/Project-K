import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router } from '@angular/router';
import { IonApp, IonRouterOutlet } from '@ionic/angular';
import { filter, map } from 'rxjs';
import { ColdStartBanner, ColdStartService } from './features/account/cold-start';

/**
 * The Лілейка theme (theme/lileyka.scss) is scoped to `.lk`, set here for the whole app, overlays
 * included. The component sheet at /ui is the one place without it: it puts Ionic as it ships next
 * to the brand version and sets `.lk` on the brand side itself.
 *
 * The shell also carries the cold-start notice: it shows over any screen, sign-in included.
 */
@Component({
  selector: 'app-root',
  imports: [IonApp, IonRouterOutlet, ColdStartBanner],
  template: `
    <ion-app [class.lk]="!onSheet()">
      <ion-router-outlet />
      <app-cold-start-banner />
    </ion-app>
  `,
})
export class App {
  private readonly router = inject(Router);
  protected readonly onSheet = toSignal(
    this.router.events.pipe(
      filter((event) => event instanceof NavigationEnd),
      map((event) => isSheet(event.urlAfterRedirects)),
    ),
    { initialValue: isSheet(this.router.url) },
  );

  constructor() {
    inject(ColdStartService).start();
  }
}

function isSheet(url: string): boolean {
  return url.split(/[?#]/)[0] === '/ui';
}
