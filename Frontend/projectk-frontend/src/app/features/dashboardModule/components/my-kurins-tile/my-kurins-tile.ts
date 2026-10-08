import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { SkeletonModule } from '@openng/optimus-ui/skeleton';
import { TagModule } from '@openng/optimus-ui/tag';
import { KurinNumberComponent } from '../../../kurinModule/components/kurin-number/kurin-number';
import { KURIN_BRANCH_LABELS } from '../../../kurinModule/models/enums/kurin-branch.enum';
import { MEMBERSHIP_KIND_LABELS } from '../../../kurinModule/models/enums/membership-kind.enum';
import { KurinScopeOption } from '../../../kurinModule/models/kurin-scope-option.model';
import { MyGroupDto } from '../../models/me.dto';

export interface KurinRow {
  option: KurinScopeOption;
  isCurrent: boolean;
}

/**
 * Where the person stands now, kurin by kurin and гурток by гурток, and a way into each. The offices
 * are shown for the kurin acted in — the only one whose offices the card has read.
 */
@Component({
  selector: 'app-my-kurins-tile',
  imports: [SkeletonModule, TagModule, KurinNumberComponent],
  templateUrl: './my-kurins-tile.html',
  styleUrl: './my-kurins-tile.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MyKurinsTileComponent {
  readonly kurins = input<KurinScopeOption[]>([]);
  readonly groups = input<MyGroupDto[]>([]);
  readonly currentKurinKey = input<string | null>(null);
  readonly currentOffices = input<string[]>([]);
  /** The kurin or гурток being opened; every row waits while the token switches. */
  readonly openingKey = input<string | null>(null);
  readonly loading = input(false);
  readonly open = output<KurinScopeOption>();
  readonly openGroup = output<MyGroupDto>();

  readonly branchLabels = KURIN_BRANCH_LABELS;
  readonly kindLabels = MEMBERSHIP_KIND_LABELS;

  readonly rows = computed<KurinRow[]>(() => {
    const current = this.currentKurinKey();
    return this.kurins()
      .map(option => ({ option, isCurrent: option.kurinKey === current }))
      .sort((a, b) => Number(b.isCurrent) - Number(a.isCurrent) || a.option.kurinNumber - b.option.kurinNumber);
  });

  /** A гурток names its kurin only when there is more than one to tell apart. */
  readonly namesKurin = computed(() => this.kurins().length > 1);

  readonly sortedGroups = computed(() =>
    [...this.groups()].sort((a, b) => Number(b.kurin.isCurrent) - Number(a.kurin.isCurrent) || a.kurin.kurinNumber - b.kurin.kurinNumber)
  );

  kurinLabel(option: KurinScopeOption): string {
    return option.namedAfter ? `к. ч. ${option.kurinNumber} ім. ${option.namedAfter}` : `к. ч. ${option.kurinNumber}`;
  }

  /** Leading one's own гурток is sitting in its провід; leading another's is being its впорядник. */
  groupRole(group: MyGroupDto): string {
    if (group.isOwn) {
      return group.isLed ? 'мій гурток · провід' : 'мій гурток';
    }
    return 'впорядник';
  }
}
