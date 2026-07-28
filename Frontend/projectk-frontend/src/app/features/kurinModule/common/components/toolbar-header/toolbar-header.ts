import { Component, inject } from '@angular/core';
import { ToolbarModule } from 'primeng/toolbar';
import { ButtonModule } from 'primeng/button';
import { AvatarModule } from 'primeng/avatar';
import { LogoutComponent } from "../../../../authModule/logout-component/logout-component";
import { AuthService } from '../../../../authModule/services/authService/auth.service';
import { PermissionService } from '../../../../authModule/services/permission.service';
import { AsyncPipe } from '@angular/common';
import { SidebarMenu } from "../sidebar-menu/sidebar-menu";
import { Router } from '@angular/router';
import { TooltipModule } from 'primeng/tooltip';
import { NotificationBell } from '../../../../notifications/components/notification-bell/notification-bell';
import { ThemeService } from '../../../../systemModule/services/theme.service';

@Component({
  selector: 'app-toolbar-header',
  imports: [ToolbarModule, ButtonModule, AvatarModule, LogoutComponent, AsyncPipe, SidebarMenu, NotificationBell, TooltipModule],
  templateUrl: './toolbar-header.html',
  styleUrl: './toolbar-header.css',
})
export class ToolbarHeader {
  private readonly authService = inject(AuthService);
  private readonly permissionService = inject(PermissionService);
  protected readonly themeService = inject(ThemeService);
  state$ = this.authService.getAuthState();
  private readonly router = inject(Router);

  sidebarVisible = false;

  toggleSidebar() {
    this.sidebarVisible = !this.sidebarVisible;
  }

  isAdmin(role?: string | null): boolean {
    return this.permissionService.isAdmin(role);
  }

  backToKurinPanel() {
    this.authService.setKurinScope(null).subscribe({
      next: () => this.router.navigate(['/panel']),
      error: () => {
        this.authService.clearKurinKey();
        this.router.navigate(['/panel']);
      }
    });
  }
}
