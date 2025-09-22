import { Component, inject } from '@angular/core';
import { ToolbarModule } from 'primeng/toolbar';
import { ButtonModule } from 'primeng/button';
import { AvatarModule } from 'primeng/avatar';
import { LogoutComponent } from "../../../../authModule/logout-component/logout-component";
import { AuthService } from '../../../../authModule/services/auth.service';
import { AsyncPipe } from '@angular/common';
import { SidebarMenu } from "../sidebar-menu/sidebar-menu";

@Component({
  selector: 'app-toolbar-header',
  imports: [ToolbarModule, ButtonModule, AvatarModule, LogoutComponent, AsyncPipe, SidebarMenu],
  templateUrl: './toolbar-header.html',
  styleUrl: './toolbar-header.scss'
})
export class ToolbarHeader {
  private readonly authService = inject(AuthService);
  state$ = this.authService.getAuthState();

  sidebarVisible: boolean = false;

  toggleSidebar() {
    this.sidebarVisible = !this.sidebarVisible;
  }
}
