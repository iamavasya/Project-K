import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { ToolbarModule } from '@openng/optimus-ui/toolbar';
import { ButtonModule } from '@openng/optimus-ui/button';
import { AvatarModule } from '@openng/optimus-ui/avatar';
import { LogoutComponent } from "../../../authModule/pages/logout/logout";
import { AuthService } from '../../../authModule/services/auth-service/auth.service';
import { PermissionService } from '../../../authModule/services/permission-service/permission.service';
import { AsyncPipe } from '@angular/common';
import { SidebarMenuComponent } from "../sidebar-menu/sidebar-menu";
import { Router } from '@angular/router';
import { TooltipModule } from '@openng/optimus-ui/tooltip';
import { MessageService } from '@openng/optimus-ui/api';
import { NotificationBellComponent } from '../../../notificationsModule/components/notification-bell/notification-bell';
import { ThemeService } from '../../../systemModule/services/theme-service/theme.service';
import { BreadcrumbComponent } from '../breadcrumb/breadcrumb';
import { KurinSwitcherComponent } from '../kurin-switcher/kurin-switcher';
import { failureDetail } from '../../../../shared/functions/failure-detail.function';

@Component({
  selector: 'app-toolbar-header',
  imports: [ToolbarModule, ButtonModule, AvatarModule, LogoutComponent, AsyncPipe, SidebarMenuComponent, NotificationBellComponent, TooltipModule, BreadcrumbComponent, KurinSwitcherComponent],
  templateUrl: './toolbar-header.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './toolbar-header.css',
})
export class ToolbarHeaderComponent {
  private readonly authService = inject(AuthService);
  private readonly permissionService = inject(PermissionService);
  protected readonly themeService = inject(ThemeService);
  private readonly messageService = inject(MessageService);
  state$ = this.authService.getAuthState();
  private readonly router = inject(Router);

  sidebarVisible = false;

  toggleSidebar() {
    this.sidebarVisible = !this.sidebarVisible;
  }

  isAdmin(): boolean {
    return this.permissionService.isAdmin();
  }

  backToKurinPanel() {
    this.authService.setKurinScope(null).subscribe({
      next: () => this.router.navigate(['/panel']),
      // Staying put is the honest outcome: only the server can widen the token's scope
      // back, so clearing it locally would leave the claim pointing at the old kurin.
      error: (error: unknown) => this.messageService.add({
        severity: 'error',
        summary: 'Не вдалося вийти з куреня',
        detail: failureDetail(error)
      })
    });
  }
}
