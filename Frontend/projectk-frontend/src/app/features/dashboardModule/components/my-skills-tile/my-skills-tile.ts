import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonModule } from '@openng/optimus-ui/button';
import { SkeletonModule } from '@openng/optimus-ui/skeleton';
import { EmptyStateComponent } from '../../../../shared/empty-state/empty-state';
import { resolveBadgeImageUrl } from '../../../kurinModule/functions/member-skills-view-mapper.function';
import { BadgeImageBlobService } from '../../../kurinModule/services/probes-and-badges/badge-image-blob.service';
import { SkillMiniCardComponent } from '../../../kurinModule/pages/member-card/components/skill-mini-card/skill-mini-card';
import { MyBadgeDto, MyGrowthDto } from '../../models/me.dto';

export interface SkillShelf {
  key: 'onReview' | 'inWork' | 'confirmed';
  label: string;
  badges: MyBadgeDto[];
}

/**
 * The person's вмілості by where they stand: handed in, begun, confirmed. The shelf that needs
 * them most — the one waiting on the впорядник — goes first.
 */
@Component({
  selector: 'app-my-skills-tile',
  imports: [RouterLink, ButtonModule, SkeletonModule, EmptyStateComponent, SkillMiniCardComponent],
  templateUrl: './my-skills-tile.html',
  styleUrl: './my-skills-tile.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MySkillsTileComponent {
  private readonly badgeImages = inject(BadgeImageBlobService);

  readonly growth = input<MyGrowthDto | null>(null);
  readonly loading = input(false);
  readonly failed = input(false);

  readonly shelves = computed<SkillShelf[]>(() => {
    const badges = this.growth()?.badges;
    if (!badges) {
      return [];
    }
    return [
      { key: 'onReview' as const, label: 'На перевірці', badges: badges.onReview },
      { key: 'inWork' as const, label: 'У роботі', badges: badges.inWork },
      { key: 'confirmed' as const, label: 'Підтверджені', badges: badges.confirmed }
    ].filter(shelf => shelf.badges.length > 0);
  });

  readonly summary = computed(() => {
    const badges = this.growth()?.badges;
    if (!badges) {
      return '';
    }
    const parts = [`підтверджено: ${badges.confirmedCount}`];
    if (badges.onReview.length) {
      parts.push(`на перевірці: ${badges.onReview.length}`);
    }
    return parts.join(' · ');
  });

  readonly cardLink = computed<unknown[] | null>(() => {
    const growth = this.growth();
    return growth ? ['/member', growth.memberKey] : null;
  });

  /**
   * Badge pictures sit behind sign-in, so a bare <img> gets a 401: the blob service fetches them with
   * the token. Reading `loaded` redraws the tile when one arrives.
   */
  imageOf(badge: MyBadgeDto): string | null {
    this.badgeImages.loaded();
    return this.badgeImages.resolveBadgeImageForDisplay(resolveBadgeImageUrl(badge.imagePath));
  }
}
