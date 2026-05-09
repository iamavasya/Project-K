import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { SkeletonModule } from 'primeng/skeleton';

@Component({
  selector: 'app-bento-tile-skeleton',
  imports: [SkeletonModule],
  templateUrl: './bento-tile-skeleton.html',
  styleUrl: './bento-tile-skeleton.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BentoTileSkeletonComponent {
  readonly variant = input<'stats' | 'list' | 'media'>('stats');
  readonly size = input<'compact' | 'regular' | 'tall'>('regular');
  readonly ribbonLabel = input('Скоро');
}
