import { Component, EventEmitter, inject, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { DrawerModule } from 'primeng/drawer';
import { ButtonModule } from 'primeng/button';
import { PanelMenuModule } from 'primeng/panelmenu';
import { MenuItem } from 'primeng/api';
import { NavigationEnd, Router } from '@angular/router';
import { MenuModule } from 'primeng/menu';
import { PermissionService } from '../../../../authModule/services/permission.service';
import { combineLatest, defer, filter, map, Observable, of, startWith } from 'rxjs';
import { AuthState } from '../../../../authModule/models/auth-state.model';
import { AsyncPipe } from '@angular/common';
import { TagModule } from 'primeng/tag';
import { environment } from '../../../../../../environments/environment';

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

  // e.g. v0.15.0-beta.pre-4 "Liberty Queen Ant" | Self-Host Environment
  // The codename is hidden for local/dev placeholder builds.
  readonly versionLabel: string = (() => {
    const code = environment.codeName && !/development/i.test(environment.codeName)
      ? ` "${environment.codeName}"`
      : '';
    return `${environment.version}${code} | ${environment.envName} Environment`;
  })();

  // defer, so the seed URL is read when something subscribes rather than when this field
  // is initialised — the menu is built before the first navigation settles.
  private readonly currentUrl$: Observable<string> = defer(() => this.router.events.pipe(
    filter(event => event instanceof NavigationEnd),
    map(() => this.router.url),
    startWith(this.router.url)
  ));

  ngOnChanges(changes: SimpleChanges) {
    if (changes['state$']) {
      this.items$ = combineLatest([this.state$, this.currentUrl$]).pipe(
        map(([state, url]) => this.markCurrent(this.buildItems(state), url))
      );
      this.email$ = this.state$.pipe(
        map(state => state?.email ?? null)
      );
      this.role$ = this.state$.pipe(
        map(state => state?.role ?? null)
      );
    }
  }
  
  /**
   * Flags the item whose routerLink best matches the current URL. PanelMenu only marks
   * headers it expands, and every item here is a leaf, so the current page would never
   * be highlighted without this. The longest match wins, otherwise "/kurin" would light
   * up alongside "/kurin/<key>/settings".
   */
  private markCurrent(items: MenuItem[], url: string): MenuItem[] {
    const path = url.split(/[?#]/)[0];
    let best: MenuItem | null = null;
    let bestLength = 0;

    for (const item of items) {
      const link = this.toPath(item.routerLink);
      if (!link || item.disabled) {
        continue;
      }
      if ((path === link || path.startsWith(`${link}/`)) && link.length > bestLength) {
        best = item;
        bestLength = link.length;
      }
    }

    return items.map(item => item === best
      ? { ...item, styleClass: 'lil-menu-item--current' }
      : item);
  }

  private toPath(routerLink: unknown): string | null {
    if (typeof routerLink === 'string') {
      return routerLink;
    }
    if (!Array.isArray(routerLink) || routerLink.length === 0) {
      return null;
    }
    return `/${routerLink.map(part => String(part)).join('/').replace(/^\/+/, '')}`;
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
          label: 'Адміністрація',
          icon: 'pi pi-lock',
          routerLink: ['/panel'],
          command: () => {
            this.close();
            this.router.navigate(['/panel']);
          }
        },
        {
          label: 'Користувачі',
          icon: 'pi pi-users',
          routerLink: ['/users'],
          command: () => {
            this.close();
            this.router.navigate(['/users']);
          }
        },
        {
          label: 'Системні налаштування',
          icon: 'pi pi-sliders-h',
          routerLink: ['/system-settings'],
          command: () => {
            this.close();
            this.router.navigate(['/system-settings']);
          }
        }
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
