import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { SelectModule } from 'primeng/select';
import { TextareaModule } from 'primeng/textarea';
import { DatePickerModule } from 'primeng/datepicker';
import { MemberAwardDto } from '../../../common/models/memberAwardDto';
import { MemberAwardLevel } from '../../../common/models/enums/member-award-level.enum';
import { BadgeProgressStatus } from '../../../common/models/enums/badge-progress-status.enum';
import { UpsertMemberAwardRequest } from '../../../common/services/member-award-service/member-award.service';

@Component({
  selector: 'app-member-awards-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonModule,
    DialogModule,
    SelectModule,
    TextareaModule,
    DatePickerModule
  ],
  templateUrl: './member-awards-dialog.html',
  styleUrl: './member-awards-dialog.css'
})
export class MemberAwardsDialogComponent {
  @Input() visible = false;
  @Input() awardToEdit: MemberAwardDto | null = null;
  @Input() existingAwards: MemberAwardDto[] = [];
  @Input() canEdit = false;
  @Input() canReview = false;

  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() save = new EventEmitter<UpsertMemberAwardRequest>();
  @Output() approve = new EventEmitter<string>();
  @Output() delete = new EventEmitter<MemberAwardDto>();

  form: FormGroup;

  levels = [
    { label: 'Перша', value: MemberAwardLevel.First },
    { label: 'Друга', value: MemberAwardLevel.Second },
    { label: 'Третя', value: MemberAwardLevel.Third },
    { label: 'Четверта', value: MemberAwardLevel.Fourth }
  ];

  constructor(private fb: FormBuilder) {
    this.form = this.fb.group({
      level: [null, Validators.required],
      dateAcquired: [null, Validators.required],
      note: ['']
    });
  }

  ngOnChanges(): void {
    if (this.visible) {
      if (this.awardToEdit) {
        this.form.patchValue({
          level: this.awardToEdit.level,
          dateAcquired: new Date(this.awardToEdit.dateAcquired),
          note: this.awardToEdit.note
        });
        this.form.get('level')?.disable();
      } else {
        this.form.reset();
        this.form.get('level')?.enable();
      }
    }
  }

  close(): void {
    this.visible = false;
    this.visibleChange.emit(this.visible);
  }

  onSave(): void {
    if (this.form.valid) {
      const formValue = this.form.getRawValue();
      const request: UpsertMemberAwardRequest = {
        memberAwardKey: this.awardToEdit?.memberAwardKey,
        level: formValue.level,
        dateAcquired: formValue.dateAcquired.toISOString(),
        note: formValue.note
      };
      this.save.emit(request);
      this.close();
    }
  }

  onApprove(): void {
    if (!this.canApproveAward || !this.awardToEdit) {
      return;
    }

    this.approve.emit(this.awardToEdit.memberAwardKey);
    this.close();
  }

  onDelete(): void {
    if (!this.canDeleteAward || !this.awardToEdit) {
      return;
    }

    this.delete.emit(this.awardToEdit);
    this.close();
  }

  get availableLevels() {
    return this.levels;
  }

  get canApproveAward(): boolean {
    return this.canReview
      && !!this.awardToEdit
      && this.normalizeStatus(this.awardToEdit.status) === BadgeProgressStatus.Submitted;
  }

  get canDeleteAward(): boolean {
    return this.canEdit && !!this.awardToEdit;
  }

  get statusLabel(): string | null {
    if (!this.awardToEdit) {
      return null;
    }

    switch (this.normalizeStatus(this.awardToEdit.status)) {
      case BadgeProgressStatus.Submitted:
        return 'Очікує підтвердження';
      case BadgeProgressStatus.Confirmed:
        return 'Підтверджено';
      case BadgeProgressStatus.Rejected:
        return 'Відхилено';
      default:
        return null;
    }
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
