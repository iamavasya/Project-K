import { Component, inject, OnInit } from '@angular/core';
import { SkeletonModule } from 'primeng/skeleton';
import { MemberDto } from '../common/models/memberDto';
import { MemberService } from '../common/services/member-service/member.service';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { DialogModule } from 'primeng/dialog';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { catchError, finalize, forkJoin, map, of, switchMap } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { BadgesCatalogService } from '../common/services/probes-and-badges/badges-catalog.service';
import { MemberProgressService } from '../common/services/probes-and-badges/member-progress.service';
import { ProbesCatalogService } from '../common/services/probes-and-badges/probes-catalog.service';
import { buildMemberSkillsSummary, normalizeBadgeProgressStatus, resolveBadgeImageUrl } from '../common/functions/memberSkillsViewMapper.function';
import { buildMemberProbeRows } from '../common/functions/memberProbeRowsViewMapper.function';
import { MemberSkillsSummaryView } from '../common/models/probes-and-badges/memberSkillsSummaryView';
import { BadgeCatalogItemDto } from '../common/models/probes-and-badges/badgeCatalogItemDto';
import { BadgeProgressDto } from '../common/models/probes-and-badges/badgeProgressDto';
import { ProbeSummaryDto } from '../common/models/probes-and-badges/probeSummaryDto';
import { ProbeProgressDto } from '../common/models/probes-and-badges/probeProgressDto';
import { MemberProbeRowView } from '../common/models/probes-and-badges/memberProbeRowView';
import { MemberSkillItemView } from '../common/models/probes-and-badges/memberSkillItemView';
import { BadgeProgressStatus } from '../common/models/enums/badge-progress-status.enum';
import { ProbeProgressStatus } from '../common/models/enums/probe-progress-status.enum';
import { SkillMiniCardComponent } from './components/skill-mini-card/skill-mini-card.component';
import { BadgeImageBlobService } from '../common/services/probes-and-badges/badge-image-blob.service';
import { AuthService } from '../../authModule/services/authService/auth.service';

@Component({
  selector: 'app-member-card',
  imports: [
    SkeletonModule,
    ButtonModule,
    TagModule,
    DialogModule,
    ConfirmDialogModule,
    FormsModule,
    InputTextModule,
    IconFieldModule,
    InputIconModule,
    SkillMiniCardComponent
  ],
  providers: [ConfirmationService],
  templateUrl: './member-card.component.html',
  styleUrl: './member-card.component.css'
})
export class MemberCardComponent implements OnInit {
  route = inject(ActivatedRoute);
  router = inject(Router);
  memberService = inject(MemberService);
  badgesCatalogService = inject(BadgesCatalogService);
  probesCatalogService = inject(ProbesCatalogService);
  memberProgressService = inject(MemberProgressService);
  badgeImageBlobService = inject(BadgeImageBlobService);
  authService = inject(AuthService);
  confirmationService = inject(ConfirmationService);
  private readonly reviewerRoles = new Set(['mentor', 'manager', 'admin']);

  member: MemberDto | null = null;
  memberKey: string | null = null;
  skillsSummary: MemberSkillsSummaryView = this.createEmptySkillsSummary();
  probeRows: MemberProbeRowView[] = this.createEmptyProbeRows();
  allBadgesCatalog: BadgeCatalogItemDto[] = [];
  badgeProgresses: BadgeProgressDto[] = [];

  isSkillsLoading = false;
  skillsLoadFailed = false;
  isProbesLoading = false;
  probesLoadFailed = false;
  isAllSkillsDialogVisible = false;
  isAddSkillDialogVisible = false;
  isSubmittingSkill = false;
  submittingBadgeId: string | null = null;
  inlineModerationBadgeId: string | null = null;

  addSkillSearchTerm = '';
  addSkillVisibleCount = 12;
  addSkillErrorMessage: string | null = null;
  addSkillSuccessMessage: string | null = null;
  inlineModerationMessage: string | null = null;
  inlineModerationSeverity: 'success' | 'warn' | 'error' = 'success';

  readonly addSkillPageSize = 12;
  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.memberKey = params.get('memberKey');
      this.refreshData();
    });
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
    this.loadProbes(this.memberKey);
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

  get completedProbesCount(): number {
    return this.probeRows.filter(row => !row.isDisabled && row.isCompleted).length;
  }

  get activeProbesCount(): number {
    return this.probeRows.filter(row => !row.isDisabled).length;
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
    this.inlineModerationMessage = null;
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

  get canOpenSkillsReview(): boolean {
    const role = this.authService.getAuthStateValue()?.role?.trim().toLowerCase() ?? '';
    const kurinKey = this.member?.kurinKey || this.authService.getAuthStateValue()?.kurinKey;
    return this.reviewerRoles.has(role) && !!kurinKey;
  }

  get canInlineModerateSkills(): boolean {
    return this.canOpenSkillsReview && !!this.memberKey;
  }

  get isInlineModerationBusy(): boolean {
    return this.inlineModerationBadgeId !== null;
  }

  canSubmitBadge(badgeId: string): boolean {
    const existing = this.badgeProgresses.find(item => item.badgeId === badgeId);
    if (!existing) {
      return true;
    }

    const normalizedStatus = normalizeBadgeProgressStatus(existing.status);
    return normalizedStatus === BadgeProgressStatus.Rejected || normalizedStatus === BadgeProgressStatus.Draft;
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
        return 'Було відхилено. Можна подати повторно';
      default:
        return 'Вже додано';
    }
  }

  getSubmitBadgeButtonLabel(badgeId: string): string {
    if (this.submittingBadgeId === badgeId) {
      return 'Додаємо...';
    }

    const existing = this.badgeProgresses.find(item => item.badgeId === badgeId);
    if (!existing) {
      return 'Додати';
    }

    const normalizedStatus = normalizeBadgeProgressStatus(existing.status);
    if (normalizedStatus === BadgeProgressStatus.Rejected) {
      return 'Подати знову';
    }

    return 'Додати';
  }

  getCatalogBadgeImageUrl(imagePath: string): string | null {
    return this.badgeImageBlobService.resolveBadgeImageForDisplay(resolveBadgeImageUrl(imagePath));
  }

  getSkillBadgeImageUrl(imageUrl: string | null): string | null {
    return this.badgeImageBlobService.resolveBadgeImageForDisplay(imageUrl);
  }

  getProbeStatusLabel(status: ProbeProgressStatus): string {
    switch (status) {
      case ProbeProgressStatus.InProgress:
        return 'В процесі';
      case ProbeProgressStatus.Completed:
        return 'Завершено';
      case ProbeProgressStatus.Verified:
        return 'Підтверджено';
      default:
        return 'Не розпочато';
    }
  }

  getProbeSummaryMeta(row: MemberProbeRowView): string {
    if (row.probeId === 'probe-2' && row.isDisabled) {
      return 'Відкриється після закриття першої проби';
    }

    if (row.probeId === 'probe-3' && row.isDisabled) {
      return 'Третя проба буде реалізована окремим етапом';
    }

    if (row.completedAtUtc) {
      return `Завершено: ${this.formatDate(row.completedAtUtc)}`;
    }

    const statusLabel = this.getProbeStatusLabel(row.status);
    if (row.pointsCount === null) {
      return statusLabel;
    }

    return `${statusLabel} · ${row.pointsCount} точок`;
  }

  openProbeDetails(row: MemberProbeRowView): void {
    if (!this.memberKey || row.isDisabled || !row.canOpenDetails) {
      return;
    }

    this.router.navigate(['/member', this.memberKey, 'probe', row.probeId]);
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

  isInlineModerationInProgress(badgeId: string): boolean {
    return this.inlineModerationBadgeId === badgeId;
  }

  approvePendingSkill(skill: MemberSkillItemView): void {
    this.inlineModerateSkill(skill, true);
  }

  removeConfirmedSkill(skill: MemberSkillItemView): void {
    this.inlineModerateSkill(skill, false);
  }

  openSkillsReviewFromDialog(): void {
    const kurinKey = this.member?.kurinKey || this.authService.getAuthStateValue()?.kurinKey;
    if (!kurinKey || !this.canOpenSkillsReview) {
      return;
    }

    this.isAllSkillsDialogVisible = false;
    this.router.navigate(['/kurin', kurinKey, 'review', 'skills']);
  }

  private inlineModerateSkill(skill: MemberSkillItemView, isApproved: boolean): void {
    if (!this.memberKey || !this.canInlineModerateSkills || this.isInlineModerationBusy) {
      return;
    }

    if (isApproved) {
      this.executeInlineModeration(skill, true);
      return;
    }

    this.confirmationService.confirm({
      header: 'Видалення підтвердженої вмілості',
      message: `Видалити підтверджену вмілість "${skill.title}"?`,
      icon: 'pi pi-exclamation-triangle',
      rejectLabel: 'Скасувати',
      rejectButtonProps: {
        label: 'Скасувати',
        severity: 'secondary',
        outlined: true
      },
      acceptButtonProps: {
        label: 'Видалити',
        severity: 'danger'
      },
      accept: () => {
        this.executeInlineModeration(skill, false);
      }
    });
  }

  private executeInlineModeration(skill: MemberSkillItemView, isApproved: boolean): void {
    if (!this.memberKey || this.isInlineModerationBusy) {
      return;
    }

    this.inlineModerationBadgeId = skill.badgeId;
    this.inlineModerationMessage = null;

    this.memberProgressService
      .reviewBadgeProgress(this.memberKey, skill.badgeId, { isApproved, note: null })
      .pipe(finalize(() => {
        this.inlineModerationBadgeId = null;
      }))
      .subscribe({
        next: () => {
          this.inlineModerationSeverity = 'success';
          this.inlineModerationMessage = isApproved
            ? `Вмілість "${skill.title}" підтверджено.`
            : `Вмілість "${skill.title}" видалено з підтверджених.`;
          this.loadSkills(this.memberKey!);
        },
        error: (error) => {
          if (error?.status === 409) {
            this.inlineModerationSeverity = 'warn';
            this.inlineModerationMessage = 'Стан вмілості вже змінено в іншому запиті. Дані оновлено.';
            this.loadSkills(this.memberKey!);
            return;
          }

          if (error?.status === 403) {
            this.inlineModerationSeverity = 'error';
            this.inlineModerationMessage = 'Немає доступу для цієї дії модерації.';
            return;
          }

          this.inlineModerationSeverity = 'error';
          this.inlineModerationMessage = 'Не вдалося виконати дію. Спробуй ще раз.';
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

  private loadProbes(memberKey: string): void {
    this.isProbesLoading = true;
    this.probesLoadFailed = false;

    let hasLoadError = false;

    this.probesCatalogService
      .getAll()
      .pipe(
        catchError((error) => {
          console.error('Error fetching probes catalog:', error);
          hasLoadError = true;
          return of([] as ProbeSummaryDto[]);
        }),
        switchMap((probes) => {
          if (probes.length === 0) {
            return of({ probes, progresses: [] as ProbeProgressDto[] });
          }

          return forkJoin(
            probes.map(probe =>
              this.memberProgressService.getProbeProgress(memberKey, probe.id).pipe(
                catchError((error) => {
                  console.error(`Error fetching probe progress for ${probe.id}:`, error);
                  hasLoadError = true;
                  return of(null);
                })
              )
            )
          ).pipe(
            map(progresses => ({
              probes,
              progresses: progresses.filter((progress): progress is ProbeProgressDto => progress !== null)
            }))
          );
        })
      )
      .subscribe(({ probes, progresses }) => {
        this.probeRows = buildMemberProbeRows(probes, progresses);
        this.probesLoadFailed = hasLoadError;
        this.isProbesLoading = false;
      });
  }

  private createEmptySkillsSummary(): MemberSkillsSummaryView {
    return {
      recentConfirmed: [],
      pendingConfirmation: [],
      orderedPreview: []
    };
  }

  private createEmptyProbeRows(): MemberProbeRowView[] {
    return buildMemberProbeRows([], []);
  }

  private formatDate(value: string): string {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return value;
    }

    return date.toLocaleDateString('uk-UA');
  }

  onEditMember() {
    this.router.navigate(
      ['/group', this.member?.groupKey, 'member', 'upsert', this.memberKey],
      { state: { fromMember: true } }
    );
  }
}
