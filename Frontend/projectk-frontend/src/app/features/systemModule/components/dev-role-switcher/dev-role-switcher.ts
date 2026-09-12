import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router } from '@angular/router';
import { filter, map } from 'rxjs';
import { MessageService } from '@openng/optimus-ui/api';
import { environment } from '../../../../../environments/environment';
import { AuthService } from '../../../authModule/services/auth-service/auth.service';
import { MemberService } from '../../../kurinModule/services/member-service/member.service';
import { DevRole, DevToolsService } from '../../services/dev-tools-service/dev-tools.service';
import { failureDetail } from '../../../../shared/functions/failure-detail.function';

interface RoleOption {
  role: Exclude<DevRole, 'Person'>;
  label: string;
  hint: string;
  icon: string;
}

const MEMBER_CARD = /^\/member\/([^/?#]+)/;

/** The member whose card a URL shows, or null when the page is not somebody's card. */
export function memberOnScreen(url: string): string | null {
  const match = MEMBER_CARD.exec(url);
  return match ? match[1] : null;
}

/**
 * A tab on the right edge of the screen, for the local tiers only, that signs an administrator
 * in as whoever holds a given office in the kurin on screen, or as the very person whose card is
 * open, and back. Testing a feature that belongs to a виховник used to mean a second browser and
 * a second set of credentials.
 *
 * Shown when the build is not a production one and the person is an administrator, or is
 * currently borrowing a seat and needs the way back.
 */
@Component({
  selector: 'app-dev-role-switcher',
  imports: [],
  templateUrl: './dev-role-switcher.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './dev-role-switcher.css'
})
export class DevRoleSwitcherComponent {
  private readonly auth = inject(AuthService);
  private readonly devTools = inject(DevToolsService);
  private readonly messageService = inject(MessageService);
  private readonly router = inject(Router);
  private readonly members = inject(MemberService);

  private readonly state = toSignal(this.auth.getAuthState(), { initialValue: this.auth.getAuthStateValue() });

  readonly open = signal(false);
  readonly busy = signal<DevRole | 'return' | null>(null);
  readonly borrowed = signal<DevRole | null>(this.devTools.borrowedRole());

  /** The person whose card is open, so that the panel can offer their own seat. */
  readonly personKey = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map(event => memberOnScreen(event.urlAfterRedirects))
    ),
    { initialValue: memberOnScreen(this.router.url) });
  readonly personName = signal<string | null>(null);

  readonly roles: RoleOption[] = [
    { role: 'Zvyazkovyi', label: 'Звʼязковий', hint: 'керує всім куренем', icon: 'pi pi-flag' },
    { role: 'Vykhovnyk', label: 'Впорядник', hint: 'веде гурток, підписує точки', icon: 'pi pi-users' },
    { role: 'Kurinnyi', label: 'Курінний', hint: 'провід куреня', icon: 'pi pi-star' },
    { role: 'Skarbnyk', label: 'Скарбник', hint: 'провід куреня, каса', icon: 'pi pi-wallet' },
    { role: 'Member', label: 'Юнак', hint: 'без уряду в курені чи КВ', icon: 'pi pi-user' }
  ];

  /** Not a production build, and either an administrator or somebody with a way back. */
  readonly visible = computed(() =>
    !environment.production && !!this.state() && (this.state()!.isAdmin || this.borrowed() !== null));

  readonly currentEmail = computed(() => this.state()?.email ?? '');

  constructor() {
    // The name is fetched only while the panel is open: the switcher must cost nothing on every page.
    effect(() => {
      const key = this.personKey();
      this.personName.set(null);
      if (!this.open() || !key) {
        return;
      }

      this.members.getByKey(key).subscribe({
        next: member => {
          if (this.personKey() === key) {
            this.personName.set(`${member.firstName} ${member.lastName}`.trim());
          }
        },
        error: () => undefined
      });
    });
  }

  toggle(): void {
    this.open.update(value => !value);
  }

  switchTo(role: Exclude<DevRole, 'Person'>): void {
    if (this.busy()) {
      return;
    }

    this.busy.set(role);
    this.devTools.impersonate(role).subscribe({
      next: () => this.reload('/kurin'),
      error: (error: unknown) => this.fail(error, 'У цьому курені ніхто з таким урядом не має акаунта.')
    });
  }

  /** Steps into the account of the person whose card is open, and lands on that same card as them. */
  switchToPerson(): void {
    const key = this.personKey();
    if (this.busy() || !key) {
      return;
    }

    this.busy.set('Person');
    this.devTools.impersonateMember(key).subscribe({
      next: () => this.reload(`/member/${key}`),
      error: (error: unknown) => this.fail(error, 'У цієї людини немає активного акаунта.')
    });
  }

  returnToAdmin(): void {
    if (this.busy()) {
      return;
    }

    this.busy.set('return');
    this.devTools.returnToAdmin().subscribe({
      next: () => this.reload('/panel'),
      error: (error: unknown) => {
        this.busy.set(null);
        // A dead ticket leaves no way back but the sign-in form; do not keep offering one.
        if ((error as { status?: number })?.status === 401) {
          this.devTools.forgetTicket();
          this.borrowed.set(null);
        }
        this.messageService.add({
          severity: 'error',
          summary: 'Не вдалося повернутись',
          detail: failureDetail(error, 'Квиток повернення прострочений. Увійди як адмін ще раз.')
        });
      }
    });
  }

  private fail(error: unknown, whenNobody: string): void {
    this.busy.set(null);
    this.messageService.add({
      severity: 'error',
      summary: 'Не вдалося перемкнути роль',
      detail: failureDetail(error, this.explain(error, whenNobody))
    });
  }

  private explain(error: unknown, whenNobody: string): string {
    switch ((error as { status?: number })?.status) {
      case 404:
        return whenNobody;
      case 409:
        return 'Це акаунт адміністратора, його не позичають.';
      default:
        return 'Спробуй ще раз.';
    }
  }

  /** A full reload: every in-memory cache belongs to the account that just left. */
  private reload(path: string): void {
    window.location.assign(path);
  }
}
