import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { MessageService } from '@openng/optimus-ui/api';
import { failureDetail } from '../../../../shared/functions/failure-detail.function';
import { TileBoardComponent } from '../../../../shared/tile-board/tile-board';
import { TILE_BOARD_KEYS } from '../../../../shared/tile-board/tile-board.models';
import { TileDefDirective } from '../../../../shared/tile-board/tile-def.directive';
import { AuthService } from '../../../authModule/services/auth-service/auth.service';
import { KurinScopeOption } from '../../../kurinModule/models/kurin-scope-option.model';
import { MemberDto } from '../../../kurinModule/models/member.dto';
import { MembershipDto } from '../../../kurinModule/models/membership.dto';
import { ROLE_DISPLAY_NAMES } from '../../../kurinModule/models/role-display-name.model';
import { LeadershipRole } from '../../../kurinModule/models/enums/leadership-role.enum';
import { MemberService } from '../../../kurinModule/services/member-service/member.service';
import { MyKurinsTileComponent } from '../../components/my-kurins-tile/my-kurins-tile';
import { MyProfileTileComponent } from '../../components/my-profile-tile/my-profile-tile';
import { greeting, todayLabel } from '../../functions/greeting.function';

/**
 * The first screen of a signed-in person: who they are and where they stand, as tiles they may
 * arrange or put away. Each tile is fed by the page and knows nothing of where its data came from,
 * so a tile is as cheap to add as a template.
 */
@Component({
  selector: 'app-dashboard',
  imports: [TileBoardComponent, TileDefDirective, MyProfileTileComponent, MyKurinsTileComponent],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly members = inject(MemberService);
  private readonly router = inject(Router);
  private readonly messages = inject(MessageService);

  readonly boardKey = TILE_BOARD_KEYS.dashboard;

  readonly member = signal<MemberDto | null>(null);
  readonly memberships = signal<MembershipDto[]>([]);
  readonly kurins = signal<KurinScopeOption[]>([]);
  readonly memberLoading = signal(true);
  readonly kurinsLoading = signal(true);
  readonly switchingTo = signal<string | null>(null);

  readonly now = new Date();
  readonly greeting = greeting(this.now);
  readonly today = todayLabel(this.now);

  readonly memberKey = computed(() => this.auth.getAuthStateValue()?.memberKey ?? null);
  readonly currentKurinKey = computed(() => this.auth.getAuthStateValue()?.kurinKey ?? null);

  readonly firstName = computed(() => this.member()?.firstName ?? '');

  /** The offices the person holds in the kurin they act in now — the card's own list, read the same way. */
  readonly currentOffices = computed(() =>
    (this.member()?.leadershipHistories ?? [])
      .filter(history => !history.endDate)
      .map(history => {
        const role = ROLE_DISPLAY_NAMES[history.role as LeadershipRole] ?? history.role;
        return history.groupName ? `${role} · ${history.groupName}` : role;
      })
  );

  ngOnInit(): void {
    const memberKey = this.memberKey();
    if (!memberKey) {
      this.memberLoading.set(false);
      this.kurinsLoading.set(false);
      return;
    }

    this.members.getByKey(memberKey).subscribe({
      next: member => { this.member.set(member); this.memberLoading.set(false); },
      error: () => this.memberLoading.set(false)
    });
    this.members.getMemberships(memberKey).subscribe({
      next: memberships => this.memberships.set(memberships),
      error: () => this.memberships.set([])
    });
    this.auth.getKurinScopeOptions().subscribe({
      next: kurins => { this.kurins.set(kurins); this.kurinsLoading.set(false); },
      error: () => this.kurinsLoading.set(false)
    });
  }

  /** A kurin is opened in the scope it belongs to: the one acted in already, or after a switch of token. */
  openKurin(option: KurinScopeOption): void {
    if (option.kurinKey === this.currentKurinKey()) {
      this.router.navigate(['/kurin', option.kurinKey]);
      return;
    }
    if (this.switchingTo()) {
      return;
    }
    this.switchingTo.set(option.kurinKey);
    this.auth.setKurinScope(option.kurinKey).subscribe({
      next: () => {
        this.switchingTo.set(null);
        this.router.navigate(['/kurin', option.kurinKey]);
      },
      error: (error: unknown) => {
        this.switchingTo.set(null);
        this.messages.add({
          severity: 'error',
          summary: 'Не вдалося перейти',
          detail: failureDetail(error, `Курінь ч. ${option.kurinNumber} лишився недосяжним. Спробуй ще раз.`)
        });
      }
    });
  }
}
