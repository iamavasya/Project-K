import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonModule } from '@openng/optimus-ui/button';
import { SkeletonModule } from '@openng/optimus-ui/skeleton';
import { TooltipModule } from '@openng/optimus-ui/tooltip';
import { EmptyStateComponent } from '../../../../shared/empty-state/empty-state';
import { SCORE_ALGORITHM_LABELS, SCORE_SOURCE_LABELS, SCORE_SOURCE_ORDER, ScoreSource } from '../../../scoreModule/models/score.enums';
import { MyScoreDto } from '../../models/me.dto';

export interface ScoreSourcePart {
  source: ScoreSource;
  label: string;
  points: number;
}

/** One kurin's row of the tile: the points, where they came from, and the гурток's place. */
export interface ScoreRow {
  score: MyScoreDto;
  parts: ScoreSourcePart[];
  rankClass: string;
  algorithm: string;
  tableLink: unknown[] | null;
}

/** The first place is primary, the rest of the podium tinted, everyone else plain — as on the table. */
function rankClassOf(place: number): string {
  if (place === 1) {
    return 'lil-rank--first';
  }
  return place > 1 && place <= 3 ? 'lil-rank--podium' : '';
}

/**
 * The person's own points this пластовий рік and where their гурток stands — the one place a youth
 * sees their points, since the table only ranks гуртки.
 */
@Component({
  selector: 'app-my-score-tile',
  imports: [RouterLink, ButtonModule, SkeletonModule, TooltipModule, EmptyStateComponent],
  templateUrl: './my-score-tile.html',
  styleUrl: './my-score-tile.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MyScoreTileComponent {
  readonly scores = input<MyScoreDto[]>([]);
  readonly loading = input(false);
  readonly failed = input(false);
  readonly namesKurin = input(false);

  readonly rows = computed<ScoreRow[]>(() =>
    this.scores().map(score => ({
      score,
      parts: SCORE_SOURCE_ORDER
        .map(source => ({ source, label: SCORE_SOURCE_LABELS[source], points: score.bySource[source] ?? 0 }))
        .filter(part => part.points !== 0),
      rankClass: rankClassOf(score.groupPlace),
      algorithm: SCORE_ALGORITHM_LABELS[score.algorithm],
      tableLink: score.kurin.isCurrent ? ['/kurin', score.kurin.kurinKey, 'score'] : null
    }))
  );

  readonly periodLabel = computed(() => this.scores()[0]?.periodLabel ?? '');

  kurinLabel(row: ScoreRow): string {
    return `к. ч. ${row.score.kurin.kurinNumber}`;
  }

  pointsClass(points: number): string {
    if (points === 0) {
      return 'lil-points--zero';
    }
    return points > 0 ? 'lil-points--plus' : 'lil-points--minus';
  }

  signed(points: number): string {
    return points > 0 ? `+${points}` : `${points}`;
  }
}
