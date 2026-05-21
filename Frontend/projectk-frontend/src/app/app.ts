import { AfterViewInit, Component, inject, signal, ViewChild } from '@angular/core';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { BreadcrumbComponent } from './features/kurinModule/common/components/breadcrumb/breadcrumb';
import { ToolbarHeader } from "./features/kurinModule/common/components/toolbar-header/toolbar-header";
import { ColdStartBannerComponent } from './features/systemModule/components/cold-start-banner/cold-start-banner';
import { MfaSetupDialogComponent } from './features/authModule/components/mfa-setup-dialog/mfa-setup-dialog.component';
import { MfaEnforcerService } from './features/authModule/services/mfa-enforcer.service';
import { ToastModule } from 'primeng/toast';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, BreadcrumbComponent, ToolbarHeader, ColdStartBannerComponent, MfaSetupDialogComponent, ToastModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements AfterViewInit {
  protected readonly title = signal('projectk-frontend');
  protected readonly isPublicShellRoute = signal(false);
  private readonly mfaEnforcer = inject(MfaEnforcerService);
  private readonly router = inject(Router);

  @ViewChild('mfaDialog') mfaDialog!: MfaSetupDialogComponent;

  constructor() {
    this.updateShellVisibility(this.router.url);
    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntilDestroyed()
      )
      .subscribe(event => this.updateShellVisibility(event.urlAfterRedirects));
  }

  ngAfterViewInit(): void {
    this.mfaEnforcer.checkAndEnforce(this.mfaDialog);
  }

  private updateShellVisibility(url: string): void {
    const path = url.split('?')[0].split('#')[0];
    const publicPrefixes = ['/', '/welcome', '/login', '/join', '/activate'];
    this.isPublicShellRoute.set(
      publicPrefixes.some(prefix => path === prefix || (prefix !== '/' && path.startsWith(`${prefix}/`)))
    );
  }
}
