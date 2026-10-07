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
import { MyDuesTileComponent } from '../../components/my-dues-tile/my-dues-tile';
import { MyDutiesTileComponent } from '../../components/my-duties-tile/my-duties-tile';
import { MyKurinsTileComponent } from '../../components/my-kurins-tile/my-kurins-tile';
import { MyProbeTileComponent } from '../../components/my-probe-tile/my-probe-tile';
import { MyProfileTileComponent } from '../../components/my-profile-tile/my-profile-tile';
import { MyScoreTileComponent } from '../../components/my-score-tile/my-score-tile';
import { MySkillsTileComponent } from '../../components/my-skills-tile/my-skills-tile';
import { MyTasksTileComponent, TaskStatusChange } from '../../components/my-tasks-tile/my-tasks-tile';
import { EventResponseChange, UpcomingEventsTileComponent } from '../../components/upcoming-events-tile/upcoming-events-tile';
import { MyDuesDto, MyDutyDto, MyEventDto, MyGrowthDto, MyScoreDto, MyTaskDto } from '../../models/me.dto';
import { MeService } from '../../services/me.service';
import { AgendaService } from '../../../kurinModule/services/agenda-service/agenda.service';
import { greeting, todayLabel } from '../../functions/greeting.function';

/**
 * The first screen of a signed-in person: who they are and where they stand, as tiles they may
 * arrange or put away. Each tile is fed by the page and knows nothing of where its data came from.
 */
@Component({
  selector: 'app-dashboard',
  imports: [
    TileBoardComponent, TileDefDirective,
    MyProfileTileComponent, MyKurinsTileComponent, UpcomingEventsTileComponent, MyTasksTileComponent,
    MyProbeTileComponent, MySkillsTileComponent, MyDuesTileComponent, MyScoreTileComponent, MyDutiesTileComponent
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly members = inject(MemberService);
  private readonly router = inject(Router);
  private readonly messages = inject(MessageService);
  private readonly me = inject(MeService);
  private readonly agenda = inject(AgendaService);

  readonly boardKey = TILE_BOARD_KEYS.dashboard;

  readonly member = signal<MemberDto | null>(null);
  readonly memberships = signal<MembershipDto[]>([]);
  readonly kurins = signal<KurinScopeOption[]>([]);
  readonly memberLoading = signal(true);
  readonly kurinsLoading = signal(true);
  readonly switchingTo = signal<string | null>(null);

  readonly events = signal<MyEventDto[]>([]);
  readonly eventsLoading = signal(true);
  readonly eventsFailed = signal(false);
  readonly respondingTo = signal<string | null>(null);

  readonly tasks = signal<MyTaskDto[]>([]);
  readonly tasksLoading = signal(true);
  readonly tasksFailed = signal(false);
  readonly movingTask = signal<string | null>(null);

  /* Tiles that are not for everyone: each appears only once its reply says there is something to show. */
  readonly growth = signal<MyGrowthDto | null>(null);
  readonly growthLoading = signal(true);
  readonly growthFailed = signal(false);
  readonly dues = signal<MyDuesDto[]>([]);
  readonly duesLoading = signal(true);
  readonly duesFailed = signal(false);
  readonly scores = signal<MyScoreDto[]>([]);
  readonly scoresLoading = signal(true);
  readonly scoresFailed = signal(false);
  readonly duties = signal<MyDutyDto[]>([]);
  readonly dutiesLoading = signal(true);
  readonly dutiesFailed = signal(false);

  /** Проба and вмілості belong to a youth of УПЮ; the reply says so, and until it comes nothing is shown. */
  readonly hasYouthProgram = computed(() => this.growth()?.hasYouthProgram ?? false);

  /** A row names its kurin only when there is more than one to tell apart. */
  readonly namesKurin = computed(() => this.kurins().length > 1);

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
    this.loadEvents();
    this.loadTasks();
    this.loadGrowth();
    this.loadDues();
    this.loadScores();
    this.loadDuties();
  }

  private loadDuties(): void {
    this.me.getDuties().subscribe({
      next: duties => { this.duties.set(duties); this.dutiesLoading.set(false); this.dutiesFailed.set(false); },
      error: () => { this.dutiesLoading.set(false); this.dutiesFailed.set(true); }
    });
  }

  private loadGrowth(): void {
    this.me.getGrowth().subscribe({
      next: growth => { this.growth.set(growth); this.growthLoading.set(false); this.growthFailed.set(false); },
      error: () => { this.growthLoading.set(false); this.growthFailed.set(true); }
    });
  }

  private loadDues(): void {
    this.me.getDues().subscribe({
      next: dues => { this.dues.set(dues); this.duesLoading.set(false); this.duesFailed.set(false); },
      error: () => { this.duesLoading.set(false); this.duesFailed.set(true); }
    });
  }

  private loadScores(): void {
    this.me.getScore().subscribe({
      next: scores => { this.scores.set(scores); this.scoresLoading.set(false); this.scoresFailed.set(false); },
      error: () => { this.scoresLoading.set(false); this.scoresFailed.set(true); }
    });
  }

  private loadEvents(): void {
    this.me.getEvents().subscribe({
      next: events => { this.events.set(events); this.eventsLoading.set(false); this.eventsFailed.set(false); },
      error: () => { this.eventsLoading.set(false); this.eventsFailed.set(true); }
    });
  }

  private loadTasks(): void {
    this.me.getTasks().subscribe({
      next: tasks => { this.tasks.set(tasks); this.tasksLoading.set(false); this.tasksFailed.set(false); },
      error: () => { this.tasksLoading.set(false); this.tasksFailed.set(true); }
    });
  }

  /** The answer lands on the row at once; every occurrence of a series shares it. */
  respond({ event, status }: EventResponseChange): void {
    if (event.myResponse === status || this.respondingTo()) {
      return;
    }
    this.respondingTo.set(event.agendaItemKey);
    this.me.setEventResponse(event.agendaItemKey, status).subscribe({
      next: () => {
        this.respondingTo.set(null);
        this.events.set(this.events().map(e => e.agendaItemKey === event.agendaItemKey ? { ...e, myResponse: status } : e));
      },
      error: (error: unknown) => {
        this.respondingTo.set(null);
        this.messages.add({ severity: 'error', summary: 'Не вдалося відповісти', detail: failureDetail(error, 'Спробуй ще раз.') });
      }
    });
  }

  /** Moved through the board's own endpoint: it answers for the kurin the token acts in, which is the only place the tile offers it. */
  moveTask({ task, status }: TaskStatusChange): void {
    if (this.movingTask()) {
      return;
    }
    this.movingTask.set(task.agendaItemKey);
    this.agenda.changeStatus(task.agendaItemKey, status).subscribe({
      next: () => {
        this.movingTask.set(null);
        this.tasks.set(status === 'Done'
          ? this.tasks().filter(t => t.agendaItemKey !== task.agendaItemKey)
          : this.tasks().map(t => t.agendaItemKey === task.agendaItemKey ? { ...t, status } : t));
      },
      error: (error: unknown) => {
        this.movingTask.set(null);
        this.messages.add({ severity: 'error', summary: 'Не вдалося змінити', detail: failureDetail(error, 'Спробуй ще раз.') });
      }
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
