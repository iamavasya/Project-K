import { Component, EventEmitter, inject, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { DrawerModule } from 'primeng/drawer';
import { ButtonModule } from 'primeng/button';
import { PanelMenuModule } from 'primeng/panelmenu';
import { MenuItem } from 'primeng/api';
import { Router } from '@angular/router';
import { MenuModule } from 'primeng/menu';
import { PermissionService } from '../../../../authModule/services/permission.service';
import { map, Observable, of } from 'rxjs';
import { AuthState } from '../../../../authModule/models/auth-state.model';
import { AsyncPipe } from '@angular/common';
import { TagModule } from 'primeng/tag';

@Component({
  selector: 'app-sidebar-menu',
  imports: [DrawerModule, ButtonModule, PanelMenuModule, MenuModule, AsyncPipe, TagModule],
  templateUrl: './sidebar-menu.html',
})
export class SidebarMenu implements OnChanges {
  private readonly router = inject(Router);
  private readonly permissionService = inject(PermissionService);
  @Input() visible = false;
  @Input() state$: Observable<AuthState | null> = of(null);
  @Output() visibleChange: EventEmitter<boolean> = new EventEmitter<boolean>();
  items$: Observable<MenuItem[]> = of([]);
  email$: Observable<string | null> = of(null);
  role$: Observable<string | null> = of(null);

  kurinKey: string | null = null;
  
  ngOnChanges(changes: SimpleChanges) {
    if (changes['state$']) {
      this.items$ = this.state$.pipe(
        map(state => this.buildItems(state))
      );
      this.email$ = this.state$.pipe(
        map(state => state?.email ?? null)
      );
      this.role$ = this.state$.pipe(
        map(state => state?.role ?? null)
      );
    }
  }
  
  private buildItems(state: AuthState | null): MenuItem[] {
    const kurinKey = state?.kurinKey ?? null;
    const memberKey = state?.memberKey ?? null;
    const role = state?.role ?? '';
    const isAdmin = this.permissionService.isAdmin(role);
    const canReviewSkills = this.permissionService.canReviewSkills(role);
    const canManageMembers = this.permissionService.canManageMembers(role);
    const canManageKurinSettings = this.permissionService.canManageKurinSettings(role);
    const disabled = !kurinKey;

    const items: MenuItem[] = [];

    if (memberKey) {
      items.push({
        label: 'Мій профіль',
        icon: 'pi pi-user',
        routerLink: ['/member', memberKey],
        command: () => {
          this.close();
          this.router.navigate(['/member', memberKey]);
        }
      });
    }

    if (kurinKey) {
      items.push(
        {
          label: 'Курінь',
          routerLink: ['/kurin'],
          command: () => {
            this.close();
            this.router.navigate(['/kurin']);
          },
          disabled
        }
      );

      if (canManageMembers) {
        items.push({ 
          label: 'Планування',
          routerLink: ['/planning', kurinKey],
          command: () => {
            this.close();
            this.router.navigate(['/planning', kurinKey]);
          }
        });
      }

      items.push(
        { label: 'Гуртки', disabled: true },
        { label: 'Всі учасники', disabled: true }
      );

      if (canReviewSkills) {
        items.push({
          label: 'Модерація вмілостей',
          routerLink: ['/kurin', kurinKey, 'review', 'skills'],
          command: () => {
            this.close();
            this.router.navigate(['/kurin', kurinKey, 'review', 'skills']);
          }
        });
      }

      if (canManageKurinSettings) {
        items.push({
          label: 'Налаштування куреня',
          icon: 'pi pi-cog',
          routerLink: ['/kurin', kurinKey, 'settings'],
          command: () => {
            this.close();
            this.router.navigate(['/kurin', kurinKey, 'settings']);
          }
        });
      }
    }

    if (isAdmin && !kurinKey) {
      items.push(
        {
          label: 'Admin Panel',
          icon: 'pi pi-lock',
          command: () => {
            this.close();
            this.router.navigate(['/panel']);
          }
        },
        { 
          label: 'Users Management',
          icon: 'pi pi-users',
          command: () => {
            this.close();
            this.router.navigate(['/users']);
          } 
        },
        { label: 'Global Settings', disabled: true }
      );
    }

    items.push({
      label: 'Налаштування акаунта',
      icon: 'pi pi-cog',
      routerLink: ['/settings/account'],
      command: () => {
        this.close();
        this.router.navigate(['/settings/account']);
      }
    });

    return items;
  }

  close() {
    this.visible = false;
    this.visibleChange.emit(this.visible);
  }

  getSeverityOnRole(role: string | null): string {
    return this.permissionService.getRoleSeverity(role);
  }
}
