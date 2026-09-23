import { Component, inject, OnChanges, SimpleChanges, ChangeDetectionStrategy, model, input, signal } from '@angular/core';
import { DrawerModule } from '@openng/optimus-ui/drawer';
import { ButtonModule } from '@openng/optimus-ui/button';
import { PanelMenuModule } from '@openng/optimus-ui/panelmenu';
import { MenuItem } from '@openng/optimus-ui/api';
import { NavigationEnd, Router } from '@angular/router';
import { MenuModule } from '@openng/optimus-ui/menu';
import { PermissionService } from '../../../authModule/services/permission-service/permission.service';
import { catchError, combineLatest, defer, filter, map, Observable, of, startWith, switchMap, tap } from 'rxjs';
import { AuthState } from '../../../authModule/models/auth-state.model';
import { AsyncPipe } from '@angular/common';
import { TagModule } from '@openng/optimus-ui/tag';
import { environment } from '../../../../../environments/environment';
import { LeadershipRole } from '../../models/enums/leadership-role.enum';
import { parseOfficeRole } from '../../functions/system-role.function';
import { getLeadershipRoleSortWeight } from '../../functions/leadership-role-order.function';
import { leadershipRoleDisplayName, leadershipRoleSeverityForRole, RoleSeverity } from '../../functions/leadership-role-display.function';
import { KurinService } from '../../services/kurin-service/kurin.service';
import { hasYouthProgram } from '../../models/enums/kurin-branch.enum';
import { ReportProblemDialogComponent } from '../../../systemModule/components/report-problem-dialog/report-problem-dialog';
import { displayCodeName } from '../../../../shared/functions/release-code-name.function';
import { WaitlistAttentionService } from '../../../adminModule/services/waitlist-attention/waitlist-attention.service';

@Component({
  selector: 'app-sidebar-menu',
  imports: [DrawerModule, ButtonModule, PanelMenuModule, MenuModule, AsyncPipe, TagModule, ReportProblemDialogComponent],
  changeDetection: ChangeDetectionStrategy.Eager,
  templateUrl: './sidebar-menu.html',
})
export class SidebarMenuComponent implements OnChanges {
  private readonly router = inject(Router);
  private readonly permissionService = inject(PermissionService);
  private readonly kurinService = inject(KurinService);
  private readonly waitlistAttention = inject(WaitlistAttentionService);
  readonly visible = model(false);
  /** «Повідомити про проблему» lives beside the menu so it can be opened from any page. */
  readonly reportVisible = signal(false);
  readonly state$ = input<Observable<AuthState | null>>(of(null));
  items$: Observable<MenuItem[]> = of([]);
  email$: Observable<string | null> = of(null);
  roleTag$: Observable<{ label: string; severity: RoleSeverity }> = of(GENERIC_MEMBER_TAG);

  kurinKey: string | null = null;

  // e.g. 0.15.0-beta.pre-4 "Liberty Queen Ant" | Self-Host Environment, or just
  // 1.0 | Production Environment for a release published without a code name.
  readonly versionLabel: string = (() => {
    const code = displayCodeName(environment.codeName);
    const quoted = code === null ? '' : ` "${code}"`;
    return `${environment.version}${quoted} | ${environment.envName} Environment`;
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
      this.items$ = combineLatest([this.state$(), this.currentUrl$]).pipe(
        // An administrator outside a kurin is the one who decides on applications; the count is
        // re-read on every navigation so the dot goes out right after the decision.
        tap(([state]) => {
          if (this.permissionService.isAdmin() && !state?.kurinKey) {
            this.waitlistAttention.refresh();
          }
        }),
        switchMap(([state, url]) => combineLatest([this.youthProgram$(state?.kurinKey ?? null), this.waitlistAttention.pending$]).pipe(
          map(([isYouthKurin, pendingWaitlist]) => this.markCurrent(this.buildItems(state, isYouthKurin, pendingWaitlist), url))
        ))
      );
      this.email$ = this.state$().pipe(
        map(state => state?.email ?? null)
      );
      this.roleTag$ = this.state$().pipe(
        map(state => this.currentRoleTag(state))
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
      ? { ...item, styleClass: [item.styleClass, 'lil-menu-item--current'].filter(Boolean).join(' ') }
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

  /**
   * Чи веде цей курінь юнацький вишкіл. Курінь читається через кеш, який і так скидається при зміні
   * скоупу, тож перемикання куреня саме собою приносить правильну відповідь.
   */
  private youthProgram$(kurinKey: string | null): Observable<boolean> {
    if (!kurinKey) {
      return of(true);
    }

    return this.kurinService.getByKey(kurinKey).pipe(
      map(kurin => hasYouthProgram(kurin.branch)),
      // Не змогли прочитати курінь — не привід ховати пункт меню, який людина має право бачити.
      catchError(() => of(true))
    );
  }

  private buildItems(state: AuthState | null, isYouthKurin = true, pendingWaitlist = 0): MenuItem[] {
    const kurinKey = state?.kurinKey ?? null;
    const memberKey = state?.memberKey ?? null;
    const isAdmin = this.permissionService.isAdmin();
    const canReviewSkills = this.permissionService.canReviewSkills();
    const canManageKurinSettings = this.permissionService.canManageKurinSettings();
    const canSeeRegistry = isAdmin
      || this.permissionService.canManageWholeKurin()
      || this.permissionService.canLeadGroups();
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

      // Реєстр — суцільний склад куреня рядками, з контактами. Це інструмент проводу, тож
      // юнакові його не показуємо; сторінка так само закрита capabilityGuard.
      if (canSeeRegistry) {
        items.push({
          label: 'Реєстр',
          icon: 'pi pi-table',
          routerLink: ['/kurin/registry'],
          command: () => {
            this.close();
            this.router.navigate(['/kurin/registry']);
          },
          disabled
        });
      }

      // Імпорт заводить склад цілого куреня — це дія Зв'язкового, не гурткового.
      if (canManageKurinSettings) {
        items.push({
          label: 'Імпорт складу',
          icon: 'pi pi-upload',
          routerLink: ['/kurin/import'],
          command: () => {
            this.close();
            this.router.navigate(['/kurin/import']);
          },
          disabled
        });
      }

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

      items.push({
        label: 'Планування',
        icon: 'pi pi-clock',
        routerLink: ['/planning', kurinKey],
        command: () => {
          this.close();
          this.router.navigate(['/planning', kurinKey]);
        }
      });

      // Гуртки та «Всі учасники» ще не реалізовані — повернути сюди, коли зʼявляться
      // сторінки, разом із іконками pi-sitemap і pi-address-book.

      if (canReviewSkills && isYouthKurin) {
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
          // The dot, not a number: «there is something to decide» is all the sidebar has to say.
          styleClass: pendingWaitlist > 0 ? 'lil-menu-item--attention' : undefined,
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

    // The guide is another site (PROJECTK_DOCS_URL is its root), so it opens in a new tab and never
    // takes the «current» mark: markCurrent only knows routerLinks. The welcome page sends people
    // to the root; from inside the app they already know what Лілейка is, so straight to the guide.
    items.push({
      label: 'Довідка',
      icon: 'pi pi-book',
      url: `${environment.docsUrl.replace(/\/+$/, '')}/user/start/what-is/`,
      target: '_blank',
      command: () => this.close()
    });

    items.push({
      label: 'Повідомити про проблему',
      icon: 'icon-bug',
      command: () => {
        this.close();
        this.reportVisible.set(true);
      }
    });

    items.push({
      label: 'Про Лілейку',
      icon: 'pi pi-info-circle',
      routerLink: ['/about'],
      command: () => {
        this.close();
        this.router.navigate(['/about']);
      }
    });

    return items;
  }

  close() {
    this.visible.set(false);
  }

  /**
   * What the viewer is called in the footer.
   *
   * The office comes first, so a Зв'язковий reads "Зв'язковий" rather than the tier "Провід
   * куреня" — the tier is what the office grants, not what the person is called, and it lumps
   * Зв'язковий together with Курінний. Colour follows the same rule the member list uses, so an
   * office is not one colour here and another there. The tiers stay as the fallback for accounts
   * that hold no office at all.
   */
  private currentRoleTag(state: AuthState | null): { label: string; severity: RoleSeverity } {
    if (this.permissionService.isAdmin()) {
      return { label: 'Адміністратор', severity: 'danger' };
    }

    const office = mostSeniorOffice(state?.roles ?? []);
    if (office) {
      return {
        label: leadershipRoleDisplayName(office),
        severity: leadershipRoleSeverityForRole(office)
      };
    }

    if (this.permissionService.canManageWholeKurin()) {
      return { label: 'Провід куреня', severity: 'warn' };
    }
    if (this.permissionService.canLeadGroups()) {
      return { label: 'Гуртковий провід', severity: 'success' };
    }
    return GENERIC_MEMBER_TAG;
  }
}

const GENERIC_MEMBER_TAG: { label: string; severity: RoleSeverity } = { label: 'Учасник', severity: 'info' };

/**
 * The office to show when an account holds several. Ordered by the same weights the member list
 * sorts by, so "most senior" means one thing across the app.
 */
function mostSeniorOffice(systemRoles: string[]): LeadershipRole | null {
  return systemRoles
    .map(parseOfficeRole)
    .filter((office): office is NonNullable<ReturnType<typeof parseOfficeRole>> => office !== null)
    .map(office => office.role)
    .sort((left, right) => getLeadershipRoleSortWeight(left) - getLeadershipRoleSortWeight(right))[0] ?? null;
}
