import { Component, inject, OnDestroy, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { SkeletonModule } from 'primeng/skeleton';
import { MemberDto } from '../common/models/memberDto';
import { MemberService } from '../common/services/member-service/member.service';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { DialogModule } from 'primeng/dialog';
import { catchError, forkJoin, of } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { BadgesCatalogService } from '../common/services/probes-and-badges/badges-catalog.service';
import { MemberProgressService } from '../common/services/probes-and-badges/member-progress.service';
import { buildMemberSkillsSummary, normalizeBadgeProgressStatus, resolveBadgeImageUrl } from '../common/functions/memberSkillsViewMapper.function';
import { MemberSkillsSummaryView } from '../common/models/probes-and-badges/memberSkillsSummaryView';
import { BadgeCatalogItemDto } from '../common/models/probes-and-badges/badgeCatalogItemDto';
import { BadgeProgressDto } from '../common/models/probes-and-badges/badgeProgressDto';
import { BadgeProgressStatus } from '../common/models/enums/badge-progress-status.enum';
import { environment } from '../../environments/environment';
import { SkillMiniCardComponent } from './components/skill-mini-card/skill-mini-card.component';

@Component({
  selector: 'app-member-card',
  imports: [
    SkeletonModule,
    ButtonModule,
    TagModule,
    DialogModule,
    FormsModule,
    InputTextModule,
    IconFieldModule,
    InputIconModule,
    SkillMiniCardComponent
  ],
  templateUrl: './member-card.component.html',
  styleUrl: './member-card.component.css'
})
export class MemberCardComponent implements OnInit, OnDestroy {
  route = inject(ActivatedRoute);
  router = inject(Router);
  memberService = inject(MemberService);
  badgesCatalogService = inject(BadgesCatalogService);
  memberProgressService = inject(MemberProgressService);
  http = inject(HttpClient);

  member: MemberDto | null = null;
  memberKey: string | null = null;
  skillsSummary: MemberSkillsSummaryView = this.createEmptySkillsSummary();
  allBadgesCatalog: BadgeCatalogItemDto[] = [];
  badgeProgresses: BadgeProgressDto[] = [];

  isSkillsLoading = false;
  skillsLoadFailed = false;
  isAllSkillsDialogVisible = false;
  isAddSkillDialogVisible = false;
  isSubmittingSkill = false;
  submittingBadgeId: string | null = null;

  addSkillSearchTerm = '';
  addSkillVisibleCount = 12;
  addSkillErrorMessage: string | null = null;
  addSkillSuccessMessage: string | null = null;

  readonly addSkillPageSize = 12;
  readonly apiOrigin = this.resolveApiOrigin();
  readonly probePlaceholderRows = [
    { id: 'probe-1', label: 'Перша проба' },
    { id: 'probe-2', label: 'Друга проба' },
    { id: 'probe-3', label: 'Третя проба' }
  ];

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.memberKey = params.get('memberKey');
      this.refreshData();
    });
  }

  ngOnDestroy(): void {
    this.releaseObjectUrls();
  }

  refreshData(): void {
    if (!this.memberKey) {
      return;
    }

    this.memberService.getByKey(this.memberKey).subscribe({
      next: (member) => {
        this.member = member;
      },
      error: (error) => {
        console.error('Error fetching member:', error);
        if (this.member?.groupKey) {
          this.router.navigate(['/group', this.member?.groupKey], { replaceUrl: true });
        }
        else this.router.navigate(['/panel'], { replaceUrl: true });
      }
    });

    this.loadSkills(this.memberKey);
  }

  get hasAnySkills(): boolean {
    return this.confirmedSkillsCount > 0 || this.pendingSkillsCount > 0;
  }

  get confirmedSkillsCount(): number {
    return this.skillsSummary.recentConfirmed.length;
  }

  get pendingSkillsCount(): number {
    return this.skillsSummary.pendingConfirmation.length;
  }

  get filteredAddSkillCandidates(): BadgeCatalogItemDto[] {
    const normalizedQuery = this.addSkillSearchTerm.trim().toLowerCase();
    const source = [...this.allBadgesCatalog].sort((left, right) => left.title.localeCompare(right.title, 'uk'));

    if (!normalizedQuery) {
      return source;
    }

    return source.filter(item =>
      item.title.toLowerCase().includes(normalizedQuery)
      || item.specialization.toLowerCase().includes(normalizedQuery)
      || item.country.toLowerCase().includes(normalizedQuery)
    );
  }

  get visibleAddSkillCandidates(): BadgeCatalogItemDto[] {
    return this.filteredAddSkillCandidates.slice(0, this.addSkillVisibleCount);
  }

  get hasMoreAddSkillCandidates(): boolean {
    return this.filteredAddSkillCandidates.length > this.addSkillVisibleCount;
  }

  openAllSkillsDialog(): void {
    this.addSkillSuccessMessage = null;
    this.addSkillErrorMessage = null;
    this.isAllSkillsDialogVisible = true;
  }

  openAddSkillDialog(): void {
    this.addSkillErrorMessage = null;
    this.addSkillSuccessMessage = null;
    this.addSkillSearchTerm = '';
    this.addSkillVisibleCount = this.addSkillPageSize;
    this.isAddSkillDialogVisible = true;
  }

  onAddSkillSearchTermChange(): void {
    this.addSkillVisibleCount = this.addSkillPageSize;
  }

  loadMoreAddSkillCandidates(): void {
    this.addSkillVisibleCount += this.addSkillPageSize;
  }

  canSubmitBadge(badgeId: string): boolean {
    const existing = this.badgeProgresses.find(item => item.badgeId === badgeId);
    return !existing;
  }

  getExistingBadgeStatusLabel(badgeId: string): string | null {
    const existing = this.badgeProgresses.find(item => item.badgeId === badgeId);
    if (!existing) {
      return null;
    }

    const normalizedStatus = normalizeBadgeProgressStatus(existing.status);

    switch (normalizedStatus) {
      case BadgeProgressStatus.Submitted:
        return 'Вже подано, очікує підтвердження';
      case BadgeProgressStatus.Confirmed:
        return 'Вже підтверджено';
      case BadgeProgressStatus.Rejected:
        return 'Було відхилено';
      default:
        return 'Вже додано';
    }
  }

  getCatalogBadgeImageUrl(imagePath: string): string | null {
    return this.resolveImageForDisplay(resolveBadgeImageUrl(imagePath));
  }

  getSkillBadgeImageUrl(imageUrl: string | null): string | null {
    return this.resolveImageForDisplay(imageUrl);
  }

  submitBadge(badgeId: string): void {
    if (!this.memberKey || this.isSubmittingSkill || !this.canSubmitBadge(badgeId)) {
      return;
    }

    this.isSubmittingSkill = true;
    this.submittingBadgeId = badgeId;
    this.addSkillErrorMessage = null;
    this.addSkillSuccessMessage = null;

    this.memberProgressService
      .submitBadgeProgress(this.memberKey, badgeId, { note: null })
      .subscribe({
        next: () => {
          this.isSubmittingSkill = false;
          this.submittingBadgeId = null;
          this.isAddSkillDialogVisible = false;
          this.addSkillSuccessMessage = 'Вмілість успішно подана на підтвердження.';
          this.loadSkills(this.memberKey!);
        },
        error: (error) => {
          console.error('Error submitting badge:', error);
          this.isSubmittingSkill = false;
          this.submittingBadgeId = null;
          this.addSkillErrorMessage = 'Не вдалося подати вмілість. Спробуй ще раз.';
        }
      });
  }

  private loadSkills(memberKey: string): void {
    this.isSkillsLoading = true;
    this.skillsLoadFailed = false;

    let hasLoadError = false;

    forkJoin({
      badges: this.badgesCatalogService.getAll(1000).pipe(
        catchError((error) => {
          console.error('Error fetching badges catalog:', error);
          hasLoadError = true;
          return of([] as BadgeCatalogItemDto[]);
        })
      ),
      progresses: this.memberProgressService.getBadgeProgresses(memberKey).pipe(
        catchError((error) => {
          console.error('Error fetching badge progresses:', error);
          hasLoadError = true;
          return of([] as BadgeProgressDto[]);
        })
      )
    }).subscribe(({ badges, progresses }) => {
      this.allBadgesCatalog = badges;
      this.badgeProgresses = progresses;
      this.skillsSummary = buildMemberSkillsSummary(progresses, badges);
      this.skillsLoadFailed = hasLoadError;
      this.isSkillsLoading = false;
    });
  }

  private createEmptySkillsSummary(): MemberSkillsSummaryView {
    return {
      recentConfirmed: [],
      pendingConfirmation: [],
      orderedPreview: []
    };
  }

  private readonly objectUrlBySourceUrl = new Map<string, string>();
  private readonly pendingImageLoads = new Set<string>();

  private resolveApiOrigin(): string {
    try {
      return new URL(environment.apiUrl).origin;
    } catch {
      return '';
    }
  }

  private resolveImageForDisplay(imageUrl: string | null): string | null {
    if (!imageUrl) {
      return null;
    }

    if (!this.isProtectedBadgesImageUrl(imageUrl)) {
      return imageUrl;
    }

    const cached = this.objectUrlBySourceUrl.get(imageUrl);
    if (cached) {
      return cached;
    }

    if (!this.pendingImageLoads.has(imageUrl)) {
      this.pendingImageLoads.add(imageUrl);
      this.http.get(imageUrl, { responseType: 'blob' }).subscribe({
        next: (blob) => {
          const objectUrl = URL.createObjectURL(blob);
          this.objectUrlBySourceUrl.set(imageUrl, objectUrl);
          this.pendingImageLoads.delete(imageUrl);
        },
        error: (error) => {
          console.error('Error fetching protected badge image:', error);
          this.pendingImageLoads.delete(imageUrl);
        }
      });
    }

    return null;
  }

  private isProtectedBadgesImageUrl(imageUrl: string): boolean {
    if (imageUrl.startsWith('/badges_images/')) {
      return true;
    }

    if (!this.apiOrigin) {
      return imageUrl.includes('/badges_images/');
    }

    return imageUrl.startsWith(`${this.apiOrigin}/badges_images/`);
  }

  private releaseObjectUrls(): void {
    for (const objectUrl of this.objectUrlBySourceUrl.values()) {
      URL.revokeObjectURL(objectUrl);
    }

    this.objectUrlBySourceUrl.clear();
    this.pendingImageLoads.clear();
  }

  onEditMember() {
    this.router.navigate(
      ['/group', this.member?.groupKey, 'member', 'upsert', this.memberKey],
      { state: { fromMember: true } }
    );
  }
}
