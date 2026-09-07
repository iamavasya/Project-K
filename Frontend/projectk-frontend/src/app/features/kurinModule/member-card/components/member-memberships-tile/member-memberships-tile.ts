import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TagModule } from '@openng/optimus-ui/tag';
import { SkeletonModule } from '@openng/optimus-ui/skeleton';
import { MembershipDto } from '../../../common/models/membershipDto';
import { KURIN_BRANCH_LABELS } from '../../../common/models/enums/kurin-branch.enum';
import { MEMBERSHIP_KIND_LABELS } from '../../../common/models/enums/membership-kind.enum';
import { ButtonModule } from '@openng/optimus-ui/button';

/**
 * Тека «Членства» з коробки людини: де вона є зараз і де була раніше.
 *
 * Курені, які людина покинула, звідси не зникають — вихід із куреня не стирає того, що там було
 * прожито. Тому теперішнє й минуле показані окремо, а не одним списком із датами.
 */
@Component({
  selector: 'app-member-memberships-tile',
  imports: [TagModule, SkeletonModule, ButtonModule],
  templateUrl: './member-memberships-tile.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './member-memberships-tile.css'
})
export class MemberMembershipsTileComponent {
  readonly memberships = input<MembershipDto[]>([]);
  readonly isLoading = input(false);
  readonly loadFailed = input(false);
  /**
   * Курінь, у якому ми зараз дієш, і чи маємо право рухати цю людину. Дії пропонуються лише тут:
   * членством у чужому курені порядкує його власний провід.
   */
  readonly scopedKurinKey = input<string | null>(null);
  readonly canManage = input(false);

  readonly moveToGroup = output<MembershipDto>();
  readonly leaveKurin = output<MembershipDto>();

  readonly branchLabels = KURIN_BRANCH_LABELS;
  readonly kindLabels = MEMBERSHIP_KIND_LABELS;

  readonly current = computed(() => this.memberships().filter(m => m.isCurrent));
  readonly past = computed(() => this.memberships().filter(m => !m.isCurrent));

  canAct(membership: MembershipDto): boolean {
    return this.canManage() && membership.isCurrent && membership.kurinKey === this.scopedKurinKey();
  }

  kurinTitle(membership: MembershipDto): string {
    return `к. ч. ${membership.kurinNumber}`;
  }

  period(membership: MembershipDto): string {
    const from = this.year(membership.joinedAtUtc);
    if (membership.isCurrent) {
      return `з ${from}`;
    }

    const to = this.year(membership.leftAtUtc);
    return from === to ? from : `${from} — ${to}`;
  }

  private year(value: string | null | undefined): string {
    if (!value) {
      return '—';
    }

    const parsed = new Date(value);
    return Number.isNaN(parsed.getTime()) ? '—' : String(parsed.getUTCFullYear());
  }
}
