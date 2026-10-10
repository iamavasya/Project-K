import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router } from '@angular/router';
import { IonApp, IonContent, IonHeader, IonMenu, IonRouterOutlet, IonTitle, IonToolbar } from '@ionic/angular';
import { filter, map } from 'rxjs';
import { AuthService } from './auth/auth.service';
import { ColdStartBanner, ColdStartService } from './features/account/cold-start';
import { NavMenu } from './nav/nav-menu';
import { NavModeService } from './nav/nav-mode.service';

/**
 * The Лілейка theme (theme/lileyka.scss) is scoped to `.lk`, set here for the whole app, overlays
 * included. The component sheet at /ui is the one place without it: it puts Ionic as it ships next
 * to the brand version and sets `.lk` on the brand side itself.
 *
 * The shell also carries the cold-start notice: it shows over any screen, sign-in included, and the
 * web's ☰ drawer for «Як у вебі» navigation (signed in only; each tab's first screen has the ☰).
 */
@Component({
  selector: 'app-root',
  imports: [IonApp, IonRouterOutlet, IonMenu, IonHeader, IonToolbar, IonTitle, IonContent, ColdStartBanner, NavMenu],
  // The drawer is a solid sheet over the page, as the web's: the glass theme leaves it see-through.
  styles: `
    ion-menu {
      --width: min(320px, 86vw);
    }
    ion-menu::part(container) {
      background: var(--lk-surface);
    }
    ion-menu ion-content {
      --background: var(--lk-surface);
    }
  `,
  template: `
    <ion-app [class.lk]="!onSheet()">
      <ion-menu contentId="main" type="overlay" [swipeGesture]="false" [disabled]="!drawer()" data-testid="drawer">
        <ion-header>
          <ion-toolbar>
            <ion-title>Лілейка</ion-title>
          </ion-toolbar>
        </ion-header>
        <ion-content>
          @if (drawer()) {
            <app-nav-menu [drawer]="true" />
          }
        </ion-content>
      </ion-menu>
      <ion-router-outlet id="main" />
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

  private readonly auth = inject(AuthService);
  private readonly nav = inject(NavModeService);
  private readonly outside = toSignal(
    this.router.events.pipe(
      filter((event) => event instanceof NavigationEnd),
      map((event) => !event.urlAfterRedirects.startsWith('/tabs')),
    ),
    { initialValue: !this.router.url.startsWith('/tabs') },
  );
  protected readonly drawer = computed(
    () => this.nav.mode() === 'web' && !!this.auth.user() && !this.onSheet() && !this.outside(),
  );

  constructor() {
    inject(ColdStartService).start();
  }
}

function isSheet(url: string): boolean {
  return url.split(/[?#]/)[0] === '/ui';
}
