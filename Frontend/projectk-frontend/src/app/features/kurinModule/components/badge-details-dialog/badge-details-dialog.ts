import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, model } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { ButtonModule } from '@openng/optimus-ui/button';
import { DialogModule } from '@openng/optimus-ui/dialog';
import { SkeletonModule } from '@openng/optimus-ui/skeleton';
import { of } from 'rxjs';
import { resolveBadgeImageUrl } from '../../functions/member-skills-view-mapper.function';
import { BadgeCatalogItemDto } from '../../models/probes-and-badges/badge-catalog-item.dto';
import { BadgeImageBlobService } from '../../services/probes-and-badges/badge-image-blob.service';
import { BadgesCatalogService } from '../../services/probes-and-badges/badges-catalog.service';

/**
 * Картка вмілості з каталогу: зображення, край, рівень, улад, спеціалізація, вимоги, вимоги до
 * інструктора, автор і дата оновлення — як на сайті каталогу. Вмілість читається за `badgeId`
 * з кешованого каталогу, тож відкривати картку можна звідусіль, де є лише ідентифікатор.
 */
@Component({
  selector: 'app-badge-details-dialog',
  imports: [NgTemplateOutlet, DialogModule, ButtonModule, SkeletonModule],
  templateUrl: './badge-details-dialog.html',
  styleUrl: './badge-details-dialog.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BadgeDetailsDialogComponent {
  readonly visible = model(false);
  readonly badgeId = input<string | null>(null);

  private readonly badgesCatalogService = inject(BadgesCatalogService);
  private readonly badgeImageBlobService = inject(BadgeImageBlobService);

  readonly badge = rxResource<BadgeCatalogItemDto | null, string | null>({
    params: () => this.badgeId(),
    stream: ({ params }) => params ? this.badgesCatalogService.getById(params) : of(null)
  });

  readonly imageUrl = computed(() => {
    const badge = this.badge.value();
    return badge ? this.badgeImageBlobService.resolveBadgeImageForDisplay(resolveBadgeImageUrl(badge.imagePath)) : null;
  });

  readonly chips = computed(() => {
    const badge = this.badge.value();
    if (!badge) {
      return [];
    }

    return [
      badge.country,
      badge.level ? `рівень ${badge.level}` : '',
      ...(badge.ulady ?? []),
      badge.specialization,
      badge.status === 'Апробаційна' ? 'апробаційна' : ''
    ].filter(chip => !!chip);
  });

  /** Older APIs send the requirements only as one text; the card shows it as it is then. */
  readonly requirementsText = computed(() => {
    const badge = this.badge.value();
    return badge && !badge.requirements?.length ? badge.seekerRequirements : '';
  });

  readonly footnote = computed(() => {
    const badge = this.badge.value();
    if (!badge) {
      return '';
    }

    const parts = [
      badge.author ? `Розробив: ${badge.author}.` : '',
      badge.lastUpdated ? `Оновлено ${formatDate(badge.lastUpdated)}.` : ''
    ];
    return parts.filter(part => !!part).join(' ');
  });

  readonly segments = textSegments;

  close(): void {
    this.visible.set(false);
  }
}

export interface TextSegment {
  text: string;
  bold: boolean;
}

/**
 * The catalogue marks bold as `**…**`, like its own site renders it. Split into plain and bold runs
 * so the template never needs innerHTML; an unpaired `**` stays as written.
 */
export function textSegments(text: string): TextSegment[] {
  return text
    .split(/\*\*(.+?)\*\*/s)
    .map((part, index) => ({ text: part, bold: index % 2 === 1 }))
    .filter(segment => segment.text.length > 0);
}

function formatDate(value: string): string {
  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(value);
  return match ? `${match[3]}.${match[2]}.${match[1]}` : value;
}
