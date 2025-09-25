import { Component, EventEmitter, inject, input, Input, OnChanges, OnInit, Output, SimpleChange, SimpleChanges } from '@angular/core';
import { DrawerModule } from 'primeng/drawer';
import { ButtonModule } from 'primeng/button';
import { PanelMenuModule } from 'primeng/panelmenu';
import { MenuItem } from 'primeng/api';
import { Router } from '@angular/router';
import { MenuModule } from 'primeng/menu';
import { AuthService } from '../../../../authModule/services/auth.service';
import { map, Observable, of } from 'rxjs';
import { AuthState } from '../../../../authModule/models/auth-state.model';
import { AsyncPipe } from '@angular/common';
import { TagModule } from 'primeng/tag';

@Component({
  selector: 'app-sidebar-menu',
  imports: [DrawerModule, ButtonModule, PanelMenuModule, MenuModule, AsyncPipe, TagModule],
  templateUrl: './sidebar-menu.html',
  styleUrl: './sidebar-menu.css'
})
export class SidebarMenu implements OnChanges {
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);
  @Input() visible: boolean = false;
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
        label: 'Kurin',
        routerLink: ['/kurin'],
        command: () => {
          this.close();
          this.router.navigate(['/kurin']);
        },
        disabled
      },
      { label: 'Groups', disabled: true },
      { label: 'All Members', disabled: true },
      { label: 'Settings', disabled: true }
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
