import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { ButtonModule } from '@openng/optimus-ui/button';
import { SkeletonModule } from '@openng/optimus-ui/skeleton';
import { TagModule } from '@openng/optimus-ui/tag';
import { KURIN_BRANCH_LABELS } from '../../../kurinModule/models/enums/kurin-branch.enum';
import { MEMBERSHIP_KIND_LABELS } from '../../../kurinModule/models/enums/membership-kind.enum';
import { KurinScopeOption } from '../../../kurinModule/models/kurin-scope-option.model';
import { MembershipDto } from '../../../kurinModule/models/membership.dto';

export interface KurinRow {
  option: KurinScopeOption;
  groupName: string | null;
  isCurrent: boolean;
}

/**
 * Where the person stands now, kurin by kurin, and a way into each. The offices are shown for the
 * kurin acted in — the only one whose offices the card has read.
 */
@Component({
  selector: 'app-my-kurins-tile',
  imports: [ButtonModule, SkeletonModule, TagModule],
  templateUrl: './my-kurins-tile.html',
  styleUrl: './my-kurins-tile.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MyKurinsTileComponent {
  readonly kurins = input<KurinScopeOption[]>([]);
  readonly memberships = input<MembershipDto[]>([]);
  readonly currentKurinKey = input<string | null>(null);
  readonly currentOffices = input<string[]>([]);
  readonly switchingTo = input<string | null>(null);
  readonly loading = input(false);
  readonly open = output<KurinScopeOption>();

  readonly branchLabels = KURIN_BRANCH_LABELS;
  readonly kindLabels = MEMBERSHIP_KIND_LABELS;

  readonly rows = computed<KurinRow[]>(() => {
    const current = this.currentKurinKey();
    const groups = new Map(
      this.memberships().filter(m => m.isCurrent).map(m => [m.kurinKey, m.groupName ?? null])
    );
    return this.kurins()
      .map(option => ({ option, groupName: groups.get(option.kurinKey) ?? null, isCurrent: option.kurinKey === current }))
      .sort((a, b) => Number(b.isCurrent) - Number(a.isCurrent) || a.option.kurinNumber - b.option.kurinNumber);
  });

  kurinLabel(option: KurinScopeOption): string {
    return option.namedAfter ? `к. ч. ${option.kurinNumber} ім. ${option.namedAfter}` : `к. ч. ${option.kurinNumber}`;
  }
}
