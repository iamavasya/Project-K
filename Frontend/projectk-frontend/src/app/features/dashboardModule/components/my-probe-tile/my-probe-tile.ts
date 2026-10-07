import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonModule } from '@openng/optimus-ui/button';
import { SkeletonModule } from '@openng/optimus-ui/skeleton';
import { TagModule } from '@openng/optimus-ui/tag';
import { EmptyStateComponent } from '../../../../shared/empty-state/empty-state';
import { MyGrowthDto, MyProbeStatus } from '../../models/me.dto';

export const PROBE_STATUS_LABELS: Record<MyProbeStatus, string> = {
  NotStarted: 'ще не почата',
  InProgress: 'в роботі',
  Completed: 'чекає перевірки',
  Verified: 'складена'
};

/**
 * The проба the person is on: how far it has come and what is next in it. Only for a youth of УПЮ —
 * the page decides that from the same reply this tile reads.
 */
@Component({
  selector: 'app-my-probe-tile',
  imports: [RouterLink, ButtonModule, SkeletonModule, TagModule, EmptyStateComponent],
  templateUrl: './my-probe-tile.html',
  styleUrl: './my-probe-tile.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MyProbeTileComponent {
  readonly growth = input<MyGrowthDto | null>(null);
  readonly loading = input(false);
  readonly failed = input(false);

  readonly probe = computed(() => this.growth()?.probe ?? null);

  readonly percent = computed(() => {
    const probe = this.probe();
    return probe && probe.totalPoints > 0 ? Math.round((probe.signedPoints / probe.totalPoints) * 100) : 0;
  });

  readonly statusLabel = computed(() => {
    const probe = this.probe();
    return probe ? PROBE_STATUS_LABELS[probe.status] : '';
  });

  readonly probeLink = computed<unknown[] | null>(() => {
    const growth = this.growth();
    const probe = this.probe();
    return growth && probe ? ['/member', growth.memberKey, 'probe', probe.probeId] : null;
  });
}
