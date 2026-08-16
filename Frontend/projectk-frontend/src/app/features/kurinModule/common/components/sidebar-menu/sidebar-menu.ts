import { Component, EventEmitter, inject, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { DrawerModule } from '@openng/optimus-ui/drawer';
import { ButtonModule } from '@openng/optimus-ui/button';
import { PanelMenuModule } from '@openng/optimus-ui/panelmenu';
import { MenuItem } from '@openng/optimus-ui/api';
import { NavigationEnd, Router } from '@angular/router';
import { MenuModule } from '@openng/optimus-ui/menu';
import { PermissionService } from '../../../../authModule/services/permission.service';
import { combineLatest, defer, filter, map, Observable, of, startWith } from 'rxjs';
import { AuthState } from '../../../../authModule/models/auth-state.model';
import { AsyncPipe } from '@angular/common';
import { TagModule } from '@openng/optimus-ui/tag';
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
        map(() => this.currentRoleLabel())
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
    const isAdmin = this.permissionService.isAdmin();
    const canReviewSkills = this.permissionService.canReviewSkills();
    const canManageMembers = this.permissionService.canManageMembers();
    const canManageKurinSettings = this.permissionService.canManageKurinSettings();
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
          icon: 'pi pi-flag',
          routerLink: ['/kurin'],
          command: () => {
            this.close();
            this.router.navigate(['/kurin']);
          },
          disabled
        }
      );

      // Календар і Задачі бачить кожен у курені — учасник бачить призначене йому,
      // провід керує. Створення обмежене на рівні сторінки/бекенду (canManageAgenda).
      items.push({
        label: 'Календар',
        icon: 'pi pi-calendar',
        routerLink: ['/calendar', kurinKey],
        command: () => {
          this.close();
          this.router.navigate(['/calendar', kurinKey]);
        }
      });

      items.push({
        label: 'Задачі',
        icon: 'pi pi-check-square',
        routerLink: ['/tasks', kurinKey],
        command: () => {
          this.close();
          this.router.navigate(['/tasks', kurinKey]);
        }
      });

      if (canManageMembers) {
        items.push({
          label: 'Планування',
          icon: 'pi pi-clock',
          routerLink: ['/planning', kurinKey],
          command: () => {
            this.close();
            this.router.navigate(['/planning', kurinKey]);
          }
        });
      }

      // Гуртки та «Всі учасники» ще не реалізовані — повернути сюди, коли зʼявляться
      // сторінки, разом із іконками pi-sitemap і pi-address-book.

      if (canReviewSkills) {
        items.push({
          label: 'Модерація вмілостей',
          icon: 'pi pi-verified',
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
      icon: 'pi pi-shield',
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

  getSeverityOnRole(_role: string | null): string {
    return this.permissionService.getRoleSeverity();
  }

  private currentRoleLabel(): string {
    if (this.permissionService.isAdmin()) {
      return 'Адміністратор';
    }
    if (this.permissionService.isManager()) {
      return 'Провід куреня';
    }
    if (this.permissionService.isMentor()) {
      return 'Гуртковий провід';
    }
    return 'Учасник';
  }
}
