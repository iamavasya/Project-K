import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, finalize, forkJoin, map, of, switchMap } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { TagModule } from 'primeng/tag';
import { SkeletonModule } from 'primeng/skeleton';
import { DialogModule } from 'primeng/dialog';
import { TextareaModule } from 'primeng/textarea';
import { AuthService } from '../../authModule/services/authService/auth.service';
import { normalizeBadgeProgressStatus, resolveBadgeImageUrl } from '../common/functions/memberSkillsViewMapper.function';
import { BadgeProgressStatus } from '../common/models/enums/badge-progress-status.enum';
import { MemberDto } from '../common/models/memberDto';
import { BadgeCatalogItemDto } from '../common/models/probes-and-badges/badgeCatalogItemDto';
import { BadgeProgressDto } from '../common/models/probes-and-badges/badgeProgressDto';
import { MemberService } from '../common/services/member-service/member.service';
import { BadgesCatalogService } from '../common/services/probes-and-badges/badges-catalog.service';
import { MemberProgressService } from '../common/services/probes-and-badges/member-progress.service';
import { BadgeImageBlobService } from '../common/services/probes-and-badges/badge-image-blob.service';

interface SkillsReviewItemView {
  reviewKey: string;
  memberKey: string;
  memberDisplayName: string;
  memberPhotoUrl: string | null;
  memberInitials: string;
  badgeId: string;
  badgeTitle: string;
  badgeImageSourceUrl: string | null;
  submittedAtUtc: string | null;
}

@Component({
  selector: 'app-skills-review-page',
  imports: [
    TableModule,
    ButtonModule,
    MessageModule,
    TagModule,
    SkeletonModule,
    DialogModule,
    TextareaModule,
    FormsModule
  ],
  templateUrl: './skills-review-page.component.html',
  styleUrl: './skills-review-page.component.css'
})
export class SkillsReviewPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);
  private readonly memberService = inject(MemberService);
  private readonly badgesCatalogService = inject(BadgesCatalogService);
  private readonly memberProgressService = inject(MemberProgressService);
  private readonly badgeImageBlobService = inject(BadgeImageBlobService);
  private readonly reviewerRoles = new Set(['mentor', 'manager', 'admin']);

  kurinKey: string | null = null;
  reviewItems: SkillsReviewItemView[] = [];

  isLoading = false;
  loadFailed = false;
  roleRestricted = false;
  activeReviewKey: string | null = null;
  isReviewDialogVisible = false;
  pendingReviewItem: SkillsReviewItemView | null = null;
  pendingReviewIsApproved = true;
  pendingReviewNote = '';

  feedbackMessage: string | null = null;
  feedbackSeverity: 'success' | 'info' | 'warn' | 'error' = 'info';

  get canReviewSkills(): boolean {
    const role = this.authService.getAuthStateValue()?.role?.trim().toLowerCase() ?? '';
    return this.reviewerRoles.has(role);
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const routeKurinKey = params.get('kurinKey');
      this.kurinKey = routeKurinKey || this.authService.getAuthStateValue()?.kurinKey || null;
      this.feedbackMessage = null;
      this.loadReviewQueue();
    });
  }

  goBackToKurin(): void {
    this.router.navigate(['/kurin']);
  }

  refreshQueue(): void {
    this.feedbackMessage = null;
    this.loadReviewQueue();
  }

  isReviewing(item: SkillsReviewItemView): boolean {
    return this.activeReviewKey === item.reviewKey;
  }

  onReview(item: SkillsReviewItemView, isApproved: boolean): void {
    if (!this.canReviewSkills || this.activeReviewKey !== null) {
      return;
    }

    this.openReviewDialog(item, isApproved);
  }

  cancelReviewDialog(): void {
    this.isReviewDialogVisible = false;
    this.pendingReviewItem = null;
    this.pendingReviewIsApproved = true;
    this.pendingReviewNote = '';
  }

  confirmReviewDialog(): void {
    const item = this.pendingReviewItem;
    if (!item || !this.canReviewSkills || this.activeReviewKey !== null) {
      return;
    }

    const isApproved = this.pendingReviewIsApproved;
    const note = this.pendingReviewNote.trim().length > 0 ? this.pendingReviewNote.trim() : null;

    this.cancelReviewDialog();

    this.activeReviewKey = item.reviewKey;
    this.feedbackMessage = null;

    this.memberProgressService
      .reviewBadgeProgress(item.memberKey, item.badgeId, { isApproved, note })
      .pipe(finalize(() => {
        this.activeReviewKey = null;
      }))
      .subscribe({
        next: () => {
          this.reviewItems = this.reviewItems.filter(current => current.reviewKey !== item.reviewKey);
          this.feedbackSeverity = 'success';
          this.feedbackMessage = isApproved
            ? `Вмілість "${item.badgeTitle}" підтверджено.`
            : `Вмілість "${item.badgeTitle}" відхилено.`;
        },
        error: (error) => {
          if (error?.status === 409) {
            this.reviewItems = this.reviewItems.filter(current => current.reviewKey !== item.reviewKey);
            this.feedbackSeverity = 'warn';
            this.feedbackMessage = 'Заявка вже опрацьована в іншому запиті. Список синхронізовано.';
            return;
          }

          if (error?.status === 403) {
            this.feedbackSeverity = 'error';
            this.feedbackMessage = 'Немає доступу до модерації цієї заявки.';
            return;
          }

          this.feedbackSeverity = 'error';
          this.feedbackMessage = 'Не вдалося виконати дію. Спробуй ще раз.';
        }
      });
  }

  getReviewDialogHeader(): string {
    return this.pendingReviewIsApproved ? 'Підтвердження вмілості' : 'Відхилення вмілості';
  }

  getReviewDialogActionLabel(): string {
    return this.pendingReviewIsApproved ? 'Підтвердити' : 'Відхилити';
  }

  formatSubmittedDate(value: string | null): string {
    if (!value) {
      return '—';
    }

    const parsedDate = Date.parse(value);
    if (Number.isNaN(parsedDate)) {
      return value;
    }

    return new Date(parsedDate).toLocaleDateString('uk-UA');
  }

  getBadgeImageUrl(imageUrl: string | null): string | null {
    return this.badgeImageBlobService.resolveBadgeImageForDisplay(imageUrl);
  }

  private loadReviewQueue(): void {
    this.roleRestricted = !this.canReviewSkills;
    this.loadFailed = false;

    if (this.roleRestricted || !this.kurinKey) {
      this.reviewItems = [];
      return;
    }

    this.isLoading = true;

    forkJoin({
      progresses: this.memberProgressService.getBadgeReviewQueue(this.kurinKey).pipe(
        catchError((error) => {
          console.error('Error loading badge review queue:', error);
          return of([] as BadgeProgressDto[]);
        })
      ),
      badges: this.badgesCatalogService.getAll(500).pipe(
        catchError((error) => {
          console.error('Error loading badges catalog for skills review:', error);
          return of([] as BadgeCatalogItemDto[]);
        })
      )
    })
      .pipe(
        finalize(() => {
          this.isLoading = false;
        })
      )
      .subscribe({
        next: ({ progresses, badges }) => {
          this.reviewItems = this.buildReviewItemsFromQueue(progresses, badges);
        },
        error: (error) => {
          console.error('Error building skills review queue:', error);
          this.loadFailed = true;
          this.reviewItems = [];
        }
      });
  }

  private openReviewDialog(item: SkillsReviewItemView, isApproved: boolean): void {
    this.pendingReviewItem = item;
    this.pendingReviewIsApproved = isApproved;
    this.pendingReviewNote = '';
    this.isReviewDialogVisible = true;
  }

  private buildReviewItemsFromQueue(
    progresses: BadgeProgressDto[],
    badges: BadgeCatalogItemDto[]
  ): SkillsReviewItemView[] {
    const badgesById = new Map<string, BadgeCatalogItemDto>(badges.map(badge => [badge.id, badge]));

    const queue: SkillsReviewItemView[] = progresses.map(progress => {
      const badge = badgesById.get(progress.badgeId);
      const memberDisplayName = [progress.memberFirstName?.trim(), progress.memberLastName?.trim()].filter(Boolean).join(' ') || progress.memberKey;
      
      return {
        reviewKey: `${progress.memberKey}:${progress.badgeId}`,
        memberKey: progress.memberKey,
        memberDisplayName,
        memberPhotoUrl: this.badgeImageBlobService.resolveBadgeImageForDisplay(progress.memberPhotoUrl || null),
        memberInitials: this.resolveInitials(memberDisplayName),
        badgeId: progress.badgeId,
        badgeTitle: badge?.title ?? progress.badgeId,
        badgeImageSourceUrl: resolveBadgeImageUrl(badge?.imagePath ?? null),
        submittedAtUtc: progress.submittedAtUtc
      };
    });

    return queue.sort((left, right) => this.toUnixTime(right.submittedAtUtc) - this.toUnixTime(left.submittedAtUtc));
  }

  private formatMemberDisplayName(member: MemberDto): string {
    const firstLast = [member.firstName?.trim(), member.lastName?.trim()].filter(Boolean).join(' ');
    return firstLast || member.memberKey;
  }

  private resolveInitials(displayName: string): string {
    const parts = displayName
      .split(' ')
      .map(part => part.trim())
      .filter(Boolean);

    if (!parts.length) {
      return '??';
    }

    return parts.slice(0, 2).map(part => part[0]?.toUpperCase() ?? '').join('');
  }

  private toUnixTime(value: string | null): number {
    if (!value) {
      return 0;
    }

    const parsed = Date.parse(value);
    return Number.isNaN(parsed) ? 0 : parsed;
  }
}