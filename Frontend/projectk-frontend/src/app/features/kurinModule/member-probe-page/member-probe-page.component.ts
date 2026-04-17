import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, finalize, forkJoin, of } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { AccordionModule } from 'primeng/accordion';
import { SkeletonModule } from 'primeng/skeleton';
import { TagModule } from 'primeng/tag';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { AuthService } from '../../authModule/services/authService/auth.service';
import { MemberService } from '../common/services/member-service/member.service';
import { ProbesCatalogService } from '../common/services/probes-and-badges/probes-catalog.service';
import { MemberProgressService } from '../common/services/probes-and-badges/member-progress.service';
import { MemberDto } from '../common/models/memberDto';
import { GroupedProbeDto } from '../common/models/probes-and-badges/groupedProbeDto';
import { ProbeProgressDto } from '../common/models/probes-and-badges/probeProgressDto';
import { MemberProbeDetailPointRowView } from '../common/models/probes-and-badges/memberProbeDetailPointRowView';
import { buildMemberProbeDetailPointRows } from '../common/functions/memberProbeDetailsViewMapper.function';
import { ProbeProgressStatus } from '../common/models/enums/probe-progress-status.enum';
import { normalizeProbeProgressStatus } from '../common/functions/memberProbeRowsViewMapper.function';

interface ProbeDetailSectionView {
  sectionId: string;
  sectionCode: string;
  sectionTitle: string;
  points: MemberProbeDetailPointRowView[];
}

@Component({
  selector: 'app-member-probe-page',
  imports: [ButtonModule, AccordionModule, SkeletonModule, TagModule, ConfirmDialogModule],
  providers: [ConfirmationService],
  templateUrl: './member-probe-page.component.html',
  styleUrl: './member-probe-page.component.css'
})
export class MemberProbePageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly memberService = inject(MemberService);
  private readonly probesCatalogService = inject(ProbesCatalogService);
  private readonly memberProgressService = inject(MemberProgressService);
  private readonly reviewerRoles = new Set(['mentor', 'manager', 'admin']);

  memberKey: string | null = null;
  probeId: string | null = null;

  member: MemberDto | null = null;
  groupedProbe: GroupedProbeDto | null = null;
  probeProgress: ProbeProgressDto | null = null;

  probeDetailPointRows: MemberProbeDetailPointRowView[] = [];
  probeSections: ProbeDetailSectionView[] = [];

  isLoading = false;
  loadFailed = false;
  hasPartialDataWarning = false;
  isUpdatingProbeStatus = false;
  reviewerActionErrorMessage: string | null = null;
  reviewerActionSuccessMessage: string | null = null;

  get canManageProbePoints(): boolean {
    const role = this.authService.getAuthStateValue()?.role?.trim().toLowerCase() ?? '';
    return this.reviewerRoles.has(role);
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.memberKey = params.get('memberKey');
      this.probeId = params.get('probeId');
      this.reviewerActionErrorMessage = null;
      this.reviewerActionSuccessMessage = null;
      this.loadData();
    });
  }

  goBackToMember(): void {
    if (!this.memberKey) {
      this.router.navigate(['/panel']);
      return;
    }

    this.router.navigate(['/member', this.memberKey]);
  }

  getProbePointSignerLabel(point: MemberProbeDetailPointRowView): string {
    if (!point.isSigned) {
      return 'Не підписано';
    }

    const name = point.signedByName?.trim();
    if (name && point.signedByRole) {
      return `${name} (${point.signedByRole})`;
    }

    if (name) {
      return name;
    }

    if (point.signedByUserKey && point.signedByRole) {
      return `${point.signedByUserKey} (${point.signedByRole})`;
    }

    if (point.signedByUserKey) {
      return point.signedByUserKey;
    }

    if (point.signedByRole) {
      return `Невідомий користувач (${point.signedByRole})`;
    }

    return 'Підписант не вказаний';
  }

  getProbePointSignedAtLabel(point: MemberProbeDetailPointRowView): string {
    if (!point.isSigned || !point.signedAtUtc) {
      return '—';
    }

    return this.formatDate(point.signedAtUtc);
  }

  getSectionPanelValue(section: ProbeDetailSectionView, sectionIndex: number): string {
    return section.sectionId || `section-${sectionIndex}`;
  }

  get canCloseProbe(): boolean {
    if (!this.canManageProbePoints || !this.memberKey || !this.probeId) {
      return false;
    }

    if (this.isUpdatingProbeStatus || !this.isCloseFlowSupportedProbe(this.probeId)) {
      return false;
    }

    if (this.isProbeClosed()) {
      return false;
    }

    return this.probeDetailPointRows.length > 0
      && this.probeDetailPointRows.every(point => point.isSigned);
  }

  get showCloseProbeInfo(): boolean {
    if (!this.probeId || !this.isCloseFlowSupportedProbe(this.probeId)) {
      return false;
    }

    return this.isProbeClosed();
  }

  get closedProbeDateLabel(): string {
    const closedAtUtc = this.probeProgress?.completedAtUtc ?? this.probeProgress?.verifiedAtUtc ?? null;
    if (!closedAtUtc) {
      return '—';
    }

    return this.formatDate(closedAtUtc);
  }

  onSignPoint(point: MemberProbeDetailPointRowView): void {
    if (!this.canManageProbePoints || point.isSigned || this.isUpdatingProbeStatus) {
      return;
    }

    this.updatePointSignature(point.pointId, true, 'Точку підписано.');
  }

  onUnsignPoint(point: MemberProbeDetailPointRowView): void {
    if (!this.canManageProbePoints || !point.isSigned || this.isUpdatingProbeStatus) {
      return;
    }

    this.confirmationService.confirm({
      header: 'Скасування підпису',
      message: `Скасувати підпис точки "${point.pointTitle}"?`,
      icon: 'pi pi-exclamation-triangle',
      rejectLabel: 'Ні',
      rejectButtonProps: {
        label: 'Ні',
        severity: 'secondary',
        outlined: true
      },
      acceptButtonProps: {
        label: 'Так, скасувати',
        severity: 'danger'
      },
      accept: () => {
        this.updatePointSignature(point.pointId, false, 'Підпис скасовано.');
      }
    });
  }

  onCloseProbe(): void {
    if (!this.memberKey || !this.probeId || !this.canCloseProbe) {
      return;
    }

    const confirmationText = this.probeId === 'probe-1'
      ? 'Після закриття першої проби відкриється друга проба для перегляду.'
      : 'Буде зафіксовано дату закриття другої проби.';

    this.confirmationService.confirm({
      header: 'Закриття проби',
      message: confirmationText,
      icon: 'pi pi-exclamation-triangle',
      rejectLabel: 'Скасувати',
      rejectButtonProps: {
        label: 'Скасувати',
        severity: 'secondary',
        outlined: true
      },
      acceptButtonProps: {
        label: 'Закрити',
        severity: 'contrast'
      },
      accept: () => {
        this.executeCloseProbe();
      }
    });
  }

  private executeCloseProbe(): void {
    if (!this.memberKey || !this.probeId || !this.canCloseProbe) {
      return;
    }

    this.isUpdatingProbeStatus = true;
    this.reviewerActionErrorMessage = null;
    this.reviewerActionSuccessMessage = null;

    this.memberProgressService
      .updateProbeProgressStatus(this.memberKey, this.probeId, {
        status: ProbeProgressStatus.Completed,
        note: null
      })
      .pipe(finalize(() => {
        this.isUpdatingProbeStatus = false;
      }))
      .subscribe({
        next: (progress) => {
          this.applyProbeProgress(progress);
          this.reviewerActionSuccessMessage = this.probeId === 'probe-1'
            ? 'Пробу здано і закрито. Друга проба тепер доступна до перегляду.'
            : 'Пробу здано і закрито. Дату закриття зафіксовано.';
        },
        error: (error) => {
          console.error('Error closing probe:', error);
          if (error?.status === 409) {
            const conflictProgress = this.extractConflictProbeProgress(error);
            if (!conflictProgress) {
              this.reviewerActionErrorMessage = 'Не вдалося закрити пробу через конфлікт. Оновлюю дані.';
              this.loadData();
              return;
            }

            this.applyProbeProgress(conflictProgress);
            const conflictStatus = normalizeProbeProgressStatus(conflictProgress.status ?? ProbeProgressStatus.NotStarted);
            if (conflictStatus === ProbeProgressStatus.Completed || conflictStatus === ProbeProgressStatus.Verified) {
              this.reviewerActionErrorMessage = null;
              this.reviewerActionSuccessMessage = 'Пробу вже закрито іншим запитом. Дані синхронізовано.';
              this.loadData();
              return;
            }

            this.reviewerActionErrorMessage = 'Пробу не вдалося закрити: не всі точки підписані або стан уже змінено.';
            this.loadData();
          } else {
            this.reviewerActionErrorMessage = 'Не вдалося закрити пробу. Спробуй ще раз.';
          }
        }
      });
  }

  private loadData(): void {
    if (!this.memberKey || !this.probeId) {
      this.loadFailed = true;
      this.isLoading = false;
      this.hasPartialDataWarning = false;
      return;
    }

    this.isLoading = true;
    this.loadFailed = false;
    this.hasPartialDataWarning = false;

    forkJoin({
      member: this.memberService.getByKey(this.memberKey).pipe(
        catchError((error) => {
          console.error('Error fetching member for probe page:', error);
          return of(null);
        })
      ),
      groupedProbe: this.probesCatalogService.getGroupedById(this.probeId).pipe(
        catchError((error) => {
          console.error(`Error fetching grouped probe ${this.probeId}:`, error);
          return of(null);
        })
      ),
      progress: this.memberProgressService.getProbeProgress(this.memberKey, this.probeId).pipe(
        catchError((error) => {
          console.error(`Error fetching probe progress ${this.probeId}:`, error);
          return of(null);
        })
      )
    }).subscribe(({ member, groupedProbe, progress }) => {
      this.member = member;
      this.groupedProbe = groupedProbe;
      this.applyProbeProgress(progress);

      this.loadFailed = groupedProbe === null;
      this.hasPartialDataWarning = groupedProbe !== null && progress === null;
      this.isLoading = false;
    });
  }

  private buildProbeSections(
    groupedProbe: GroupedProbeDto | null,
    pointRows: MemberProbeDetailPointRowView[]
  ): ProbeDetailSectionView[] {
    if (!groupedProbe) {
      return [];
    }

    return groupedProbe.sections.map(section => ({
      sectionId: section.id,
      sectionCode: section.code,
      sectionTitle: section.title,
      points: pointRows.filter(row => row.sectionId === section.id)
    }));
  }

  private formatDate(value: string): string {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return value;
    }

    return date.toLocaleDateString('uk-UA');
  }

  private applyProbeProgress(progress: ProbeProgressDto | null): void {
    this.probeProgress = progress;
    this.probeDetailPointRows = buildMemberProbeDetailPointRows(this.groupedProbe, progress);
    this.probeSections = this.buildProbeSections(this.groupedProbe, this.probeDetailPointRows);
  }

  private isProbeClosed(): boolean {
    const normalizedStatus = normalizeProbeProgressStatus(this.probeProgress?.status ?? ProbeProgressStatus.NotStarted);
    return normalizedStatus === ProbeProgressStatus.Completed || normalizedStatus === ProbeProgressStatus.Verified;
  }

  private isCloseFlowSupportedProbe(probeId: string): boolean {
    return probeId === 'probe-1' || probeId === 'probe-2';
  }

  private updatePointSignature(pointId: string, isSigned: boolean, successMessage: string): void {
    if (!this.memberKey || !this.probeId || this.isUpdatingProbeStatus || !pointId) {
      return;
    }

    this.isUpdatingProbeStatus = true;
    this.reviewerActionErrorMessage = null;
    this.reviewerActionSuccessMessage = null;

    const request = { note: null };
    const operation$ = isSigned
      ? this.memberProgressService.signProbePoint(this.memberKey, this.probeId, pointId, request)
      : this.memberProgressService.unsignProbePoint(this.memberKey, this.probeId, pointId, request);

    operation$
      .pipe(finalize(() => {
        this.isUpdatingProbeStatus = false;
      }))
      .subscribe({
        next: (progress) => {
          this.applyProbeProgress(progress);
          this.reviewerActionSuccessMessage = successMessage;
        },
        error: (error) => {
          console.error('Error updating probe point signature:', error);
          if (error?.status === 409) {
            const conflictProgress = this.extractConflictProbeProgress(error);
            if (!conflictProgress) {
              this.reviewerActionErrorMessage = 'Не вдалося оновити підпис точки через конфлікт. Оновлюю дані.';
              this.loadData();
              return;
            }

            this.applyProbeProgress(conflictProgress);
            this.reviewerActionErrorMessage = 'Конфлікт оновлення. Дані синхронізовано, перевір поточний стан точки.';
            this.loadData();
          } else {
            this.reviewerActionErrorMessage = 'Не вдалося оновити підпис точки. Спробуй ще раз.';
          }
        }
      });
  }

  private extractConflictProbeProgress(error: unknown): ProbeProgressDto | null {
    if (!error || typeof error !== 'object') {
      return null;
    }

    const payload = (error as { error?: unknown }).error;

    if (Array.isArray(payload) && payload.length > 1) {
      const maybeProgress = payload[1];
      if (maybeProgress && typeof maybeProgress === 'object') {
        return maybeProgress as ProbeProgressDto;
      }
    }

    if (payload && typeof payload === 'object') {
      const candidate = payload as Partial<ProbeProgressDto>;
      if (candidate.probeId && candidate.status !== undefined) {
        return candidate as ProbeProgressDto;
      }
    }

    return null;
  }
}
