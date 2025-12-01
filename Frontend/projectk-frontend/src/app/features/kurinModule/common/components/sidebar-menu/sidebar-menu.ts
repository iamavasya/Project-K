import { Component, EventEmitter, inject, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { DrawerModule } from 'primeng/drawer';
import { ButtonModule } from 'primeng/button';
import { PanelMenuModule } from 'primeng/panelmenu';
import { MenuItem } from 'primeng/api';
import { Router } from '@angular/router';
import { MenuModule } from 'primeng/menu';
import { AuthService } from '../../../../authModule/services/authService/auth.service';
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
  private readonly authService = inject(AuthService);
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
        map(state => this.buildItems(state?.kurinKey ?? null))
      );
      this.email$ = this.state$.pipe(
        map(state => state?.email ?? null)
      );
      this.role$ = this.state$.pipe(
        map(state => state?.role ?? null)
      );
    }
  }
  
  private buildItems(kurinKey: string | null): MenuItem[] {
    const disabled = !kurinKey;
    return [
      {
        label: 'Курінь',
        routerLink: ['/kurin'],
        command: () => {
          this.close();
          this.router.navigate(['/kurin']);
        },
        disabled,
        visible: !!kurinKey
      },
      { label: 'Планування',
        routerLink: ['/planning', kurinKey],
        command: () => {
          this.close();
          this.router.navigate(['/planning', kurinKey]);
        },
        visible: !!kurinKey
      },
      { label: 'Гуртки', disabled: true, visible: !!kurinKey },
      { label: 'Всі учасники', disabled: true, visible: !!kurinKey },
      { label: 'Налаштування', disabled: true, visible: !!kurinKey },
      {
        label: 'Panel',
        visible: !kurinKey,
        command: () => {
          this.close();
          this.router.navigate(['/panel']);
        }
      },
      { 
        label: 'Users',
        visible: !kurinKey,
        command: () => {
          this.close();
          this.router.navigate(['/users']);
        } 
      },
      { label: 'Global Settings', visible: !kurinKey, disabled: true },
    ];
  }

  close() {
    this.visible = false;
    this.visibleChange.emit(this.visible);
  }

  getSeverityOnRole(role: string | null): string {
    switch (role?.toLowerCase()) {
      case 'admin':
        return 'danger';
      case 'manager':
        return 'warning';
      case 'mentor':
        return 'success';
      default:
        return 'info';
    }
  }
}
