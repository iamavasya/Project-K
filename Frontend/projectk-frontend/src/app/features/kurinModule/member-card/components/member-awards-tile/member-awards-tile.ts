import { Component, Input, inject, ChangeDetectionStrategy, output } from '@angular/core';

import { ButtonModule } from '@openng/optimus-ui/button';
import { ConfirmDialogModule } from '@openng/optimus-ui/confirmdialog';
import { ConfirmationService } from '@openng/optimus-ui/api';
import { MemberAwardDto } from '../../../common/models/memberAwardDto';
import { MemberAwardLevel } from '../../../common/models/enums/member-award-level.enum';
import { BadgeProgressStatus } from '../../../common/models/enums/badge-progress-status.enum';
import { MemberAwardsDialogComponent } from '../member-awards-dialog/member-awards-dialog';
import { UpsertMemberAwardRequest } from '../../../common/services/member-award-service/member-award.service';
import { BadgeImageBlobService } from '../../../common/services/probes-and-badges/badge-image-blob.service';
import { environment } from '../../../../../../environments/environment';
import { dateOnlyTime } from '../../../common/functions/toDateOnlyString.function';

interface AwardGroup {
  level: MemberAwardLevel;
  count: number;
  latest: MemberAwardDto;
}

@Component({
  selector: 'app-member-awards-tile',
  imports: [
    ButtonModule,
    ConfirmDialogModule,
    MemberAwardsDialogComponent
],
  providers: [ConfirmationService],
  templateUrl: './member-awards-tile.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './member-awards-tile.css'
})
export class MemberAwardsTileComponent {
  @Input() awards: MemberAwardDto[] = [];
  @Input() canEdit = false;
  @Input() canReview = false;

  readonly saveAward = output<UpsertMemberAwardRequest>();
  readonly deleteAward = output<string>();
  readonly reviewAward = output<{
    awardKey: string;
    isApproved: boolean;
}>();

  dialogVisible = false;
  selectedAward: MemberAwardDto | null = null;

  private readonly confirmationService = inject(ConfirmationService);
  private readonly badgeImageBlobService = inject(BadgeImageBlobService);

  getAwardTitle(level: MemberAwardLevel): string {
    switch (level) {
      case MemberAwardLevel.First: return 'Перше відзначення';
      case MemberAwardLevel.Second: return 'Друге відзначення';
      case MemberAwardLevel.Third: return 'Третє відзначення';
      case MemberAwardLevel.Fourth: return 'Четверте відзначення';
      default: return 'Невідоме відзначення';
    }
  }

  get groupedAwards() {
    const map = new Map<string, AwardGroup>();
    for (const award of this.awards.filter(item => this.isVisibleAward(item))) {
      const existing = map.get(award.level);
      if (existing) {
        existing.count++;
        if (dateOnlyTime(award.dateAcquired) > dateOnlyTime(existing.latest.dateAcquired)) {
          existing.latest = award;
        }
      } else {
        map.set(award.level, { level: award.level, count: 1, latest: award });
      }
    }
    return Array.from(map.values()).sort((a, b) => this.getAwardLevelWeight(a.level) - this.getAwardLevelWeight(b.level));
  }

  trackAwardGroup(_: number, group: AwardGroup): MemberAwardLevel {
    return group.level;
  }

  getAwardImageUrl(award: MemberAwardDto): string | null {
    const status = this.normalizeStatus(award.status);
    const level = this.getAwardLevelWeight(award.level);
    const colored = status === BadgeProgressStatus.Confirmed;
    const sourceUrl = award.imageUrl || `${environment.apiUrl}/awards/images/${level}?colored=${colored}`;
    return this.badgeImageBlobService.resolveBadgeImageForDisplay(sourceUrl);
  }

  isPendingConfirmation(award: MemberAwardDto): boolean {
    return this.normalizeStatus(award.status) === BadgeProgressStatus.Submitted;
  }

  private getAwardLevelWeight(level: MemberAwardLevel): number {
    switch (level) {
      case MemberAwardLevel.Fourth: return 4;
      case MemberAwardLevel.Third: return 3;
      case MemberAwardLevel.Second: return 2;
      case MemberAwardLevel.First: return 1;
      default: return 0;
    }
  }

  openDialog(award?: MemberAwardDto): void {
    if (!this.canEdit) return;
    this.selectedAward = award || null;
    this.dialogVisible = true;
  }

  onSave(request: UpsertMemberAwardRequest): void {
    this.saveAward.emit(request);
  }

  confirmDelete(award: MemberAwardDto): void {
    this.confirmationService.confirm({
      message: 'Ви впевнені, що хочете видалити це відзначення?',
      header: 'Підтвердження видалення',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.deleteAward.emit(award.memberAwardKey);
      }
    });
  }

  approveAward(awardKey: string): void {
    this.reviewAward.emit({ awardKey, isApproved: true });
  }

  private isVisibleAward(award: MemberAwardDto): boolean {
    const status = this.normalizeStatus(award.status);
    return status === BadgeProgressStatus.Submitted || status === BadgeProgressStatus.Confirmed;
  }

  private normalizeStatus(status: BadgeProgressStatus | string | number): BadgeProgressStatus {
    if (typeof status === 'number' && BadgeProgressStatus[status] !== undefined) {
      return status as BadgeProgressStatus;
    }

    if (typeof status === 'string') {
      const enumValue = BadgeProgressStatus[status as keyof typeof BadgeProgressStatus];
      if (typeof enumValue === 'number') {
        return enumValue;
      }
    }

    return BadgeProgressStatus.Draft;
  }
}
