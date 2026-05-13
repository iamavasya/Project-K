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

@Component({
  selector: 'app-toolbar-header',
  imports: [ToolbarModule, ButtonModule, AvatarModule, LogoutComponent, AsyncPipe, SidebarMenu],
  templateUrl: './toolbar-header.html',
})
export class ToolbarHeader {
  private readonly authService = inject(AuthService);
  private readonly permissionService = inject(PermissionService);
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
    this.authService.clearKurinKey();
    this.router.navigate(['/panel']);
  }
}
