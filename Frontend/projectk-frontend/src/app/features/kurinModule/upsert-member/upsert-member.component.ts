import { Component, inject, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FloatLabelModule } from 'primeng/floatlabel';
import { FormGroup, FormsModule, NgForm } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { InputMaskModule } from 'primeng/inputmask';
import { DatePickerModule } from 'primeng/datepicker';
import { MemberService } from '../common/services/member-service/member.service';
import { ButtonModule } from 'primeng/button';
import { MemberDto } from '../common/models/memberDto';
import { UpsertMemberDto } from '../common/models/requests/member/upsertMemberDto';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { MinAgeValidatorDirective } from "../common/directives/min-age-validator/min-age.validator";
import { FileSelectEvent, FileUploadModule } from 'primeng/fileupload';
import { ImageCropperComponent, ImageCroppedEvent } from 'ngx-image-cropper';
import { DialogModule } from 'primeng/dialog';
import { base64ToBlob } from '../common/functions/base64ToBlob.function';
import { Location } from '@angular/common';
import { AccordionModule } from 'primeng/accordion';
import { PlastLevelHistoryDto } from '../common/models/plastLevelHistoryDto';
import { PlastLevel } from '../common/models/enums/plast-level.enum';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { toDateOnlyString } from '../common/functions/toDateOnlyString.function';
import { MemberWarningService } from '../common/services/member-warning-service/member-warning.service';
import { MemberWarningDto } from '../common/models/memberWarningDto';
import { MemberWarningLevel } from '../common/models/enums/member-warning-level.enum';
import { AuthService } from '../../authModule/services/authService/auth.service';
import { concatMap, from, Observable, of, toArray } from 'rxjs';

@Component({
  selector: 'app-upsert-member',
  imports: [FloatLabelModule, FormsModule, InputTextModule, InputMaskModule, DatePickerModule, ButtonModule, ConfirmDialogModule, MinAgeValidatorDirective, FileUploadModule, ImageCropperComponent, DialogModule, AccordionModule, ToggleSwitchModule],
  providers: [ConfirmationService],
  templateUrl: './upsert-member.component.html',
  styleUrl: './upsert-member.component.css'
})
export class UpsertMemberComponent implements OnInit {
  @ViewChild('form') form!: NgForm;
  activeAccordionPanels: string[] = ['0'];

  member: MemberDto = {
    memberKey: '',
    groupKey: '',
    kurinKey: '',
    firstName: '',
    middleName: '',
    lastName: '',
    email: '',
    phoneNumber: '',
    dateOfBirth: null,
    plastLevelHistories: [],
    leadershipHistories: [],
    profilePhotoUrl: null,
  };

  memberKey = '';
  groupKey = '';
  kurinKey = '';
  createUserAccount = false;

  route = inject(ActivatedRoute);
  router = inject(Router);
  location = inject(Location);
  memberService = inject(MemberService);
  memberWarningService = inject(MemberWarningService);
  authService = inject(AuthService);
  confirmationService = inject(ConfirmationService);

  private cameFromMember = false;

  isCreate = false;
  canManageWarnings = false;

  plastLevelMap: Record<PlastLevel, PlastLevelHistoryDto> = {} as Record<PlastLevel, PlastLevelHistoryDto>;
  readonly PlastLevel = PlastLevel;
  readonly levelsConfig: { level: PlastLevel, label: string }[] = [
    { level: PlastLevel.Entry, label: 'Вступ' },
    { level: PlastLevel.Uchasnyk, label: 'Уч.' },
    { level: PlastLevel.Rozviduvach, label: 'Розвд.' },
    { level: PlastLevel.Skob, label: 'Скоб' },
    { level: PlastLevel.HetmanskiySkob, label: 'Гетьм. скоб' },
    { level: PlastLevel.Starshoplastun, label: 'Старшопластун' },
    { level: PlastLevel.Senior, label: 'пл. сен.' },
    { level: PlastLevel.SeniorPratsi, label: 'пл. сен. пр.' },
    { level: PlastLevel.SeniorDovirja, label: 'пл. сен. дов.' },
    { level: PlastLevel.SeniorKerivnytstva, label: 'пл. сен. кер.' },
  ];

  plastLevelHistories: PlastLevelHistoryDto[] = [];
  showUpuLevels = false;
  showUspLevels = false;
  showUpsLevels = false;
  memberWarnings: MemberWarningDto[] = [];
  
  warningsToAssign = new Set<MemberWarningLevel>();
  warningsToCancel = new Set<string>(); // memberWarningKeys

  readonly MemberWarningLevel = MemberWarningLevel;
  readonly warningLevels = [
    { level: MemberWarningLevel.Level1, label: 'Перша пересторога (3 місяці)' },
    { level: MemberWarningLevel.Level2, label: 'Друга пересторога (6 місяців)' },
    { level: MemberWarningLevel.Level3, label: 'Третя пересторога (12 місяців)' }
  ];

  imageFile?: File;
  croppedImage = '';
  croppedFile: File | null = null;
  displayCropper = false;
  fileToUpload: Blob | null = null;
  removeProfilePhoto = false;

  private objectUrlToRevoke: string | null = null;

  defaultDateFormat = 'yy-mm-dd';

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.memberKey = params.get('memberKey') ?? '';
      this.groupKey = params.get('groupKey') ?? '';
      this.kurinKey = params.get('kurinKey') ?? '';
    });
    if (this.memberKey) {
      this.loadData();
      this.isCreate = false;
    } else {
      this.ensurePlastLevelMap();
      this.setupAccordionAndToggles();
      this.isCreate = true;
    }
    const navState = (this.router.getCurrentNavigation()?.extras.state as { fromMember?: boolean } | undefined) ?? history.state;
    this.cameFromMember = navState?.fromMember === true;
    this.canManageWarnings = this.resolveCanManageWarnings();
  }

  private loadData(): void {
    this.memberService.getByKey(this.memberKey).subscribe({
      next: (member) => {
        this.member = member;
        this.memberWarnings = member.warnings ?? [];
        if (this.member.dateOfBirth) {
          this.member.dateOfBirth = new Date(this.member.dateOfBirth);
        }
        if (!this.member.plastLevelHistories) {
          this.member.plastLevelHistories = [];
        } else {
          this.member.plastLevelHistories.forEach(h => {
            if (h.dateAchieved) {
              h.dateAchieved = new Date(h.dateAchieved);
            }
          });
        }
        this.ensurePlastLevelMap();
        this.setupAccordionAndToggles();
      },
      error: (error) => {
        console.error('Error fetching member:', error);
        this.router.navigate(['/group', this.groupKey], { replaceUrl: true });
      }
    });
  }

  private setupAccordionAndToggles() {
    const hasAnyPlastLevel = this.levelsConfig.some(config => this.plastLevelMap[config.level]?.dateAchieved != null);
    if (hasAnyPlastLevel && !this.activeAccordionPanels.includes('1')) {
      this.activeAccordionPanels.push('1');
    }

    if (this.memberWarnings.some(w => this.isWarningActive(w, new Date())) && !this.activeAccordionPanels.includes('2')) {
      this.activeAccordionPanels.push('2');
    }

    this.showUpuLevels = [PlastLevel.Rozviduvach, PlastLevel.Skob, PlastLevel.HetmanskiySkob].some(l => this.plastLevelMap[l]?.dateAchieved != null);
    this.showUspLevels = [PlastLevel.Starshoplastun].some(l => this.plastLevelMap[l]?.dateAchieved != null);
    this.showUpsLevels = [PlastLevel.Senior, PlastLevel.SeniorPratsi, PlastLevel.SeniorDovirja, PlastLevel.SeniorKerivnytstva].some(l => this.plastLevelMap[l]?.dateAchieved != null);
  }

  markFormDirty() {
    this.form?.form.markAsDirty();
  }

  onUpuToggle(value: boolean) {
    this.showUpuLevels = value;
    this.markFormDirty();
    if (!value) {
      this.plastLevelMap[PlastLevel.Rozviduvach].dateAchieved = null;
      this.plastLevelMap[PlastLevel.Skob].dateAchieved = null;
      this.plastLevelMap[PlastLevel.HetmanskiySkob].dateAchieved = null;
    }
  }

  onUspToggle(value: boolean) {
    this.showUspLevels = value;
    this.markFormDirty();
    if (!value) {
      this.plastLevelMap[PlastLevel.Starshoplastun].dateAchieved = null;
    }
  }

  onUpsToggle(value: boolean) {
    this.showUpsLevels = value;
    this.markFormDirty();
    if (!value) {
      this.plastLevelMap[PlastLevel.Senior].dateAchieved = null;
      this.plastLevelMap[PlastLevel.SeniorPratsi].dateAchieved = null;
      this.plastLevelMap[PlastLevel.SeniorDovirja].dateAchieved = null;
      this.plastLevelMap[PlastLevel.SeniorKerivnytstva].dateAchieved = null;
    }
  }

  private resolveCanManageWarnings(): boolean {
    const role = this.authService.getAuthStateValue()?.role?.trim().toLowerCase() ?? '';
    return role === 'mentor' || role === 'manager' || role === 'admin';
  }

  private isWarningActive(warning: MemberWarningDto, now: Date): boolean {
    if (warning.revokedAtUtc) {
      return false;
    }

    const expiresAt = Date.parse(warning.expiresAtUtc);
    return !Number.isNaN(expiresAt) && expiresAt > now.getTime();
  }

  getActiveWarning(level: MemberWarningLevel): MemberWarningDto | null {
    if (this.warningsToAssign.has(level)) {
      return {
        memberWarningKey: 'dummy',
        memberKey: this.memberKey,
        level: level,
        issuedAtUtc: new Date().toISOString(),
        expiresAtUtc: new Date(Date.now() + 86400000).toISOString(),
        issuedByUserKey: this.authService.getAuthStateValue()?.userKey ?? ''
      } as MemberWarningDto;
    }

    const now = new Date();
    const warning = this.memberWarnings.find(w => w.level === level && this.isWarningActive(w, now));
    if (warning && !this.warningsToCancel.has(warning.memberWarningKey)) {
      return warning;
    }

    return null;
  }

  isWarningChecked(level: MemberWarningLevel): boolean {
    return this.getActiveWarning(level) !== null;
  }

  canSelectWarning(level: MemberWarningLevel): boolean {
    if (!this.canManageWarnings) {
      return false;
    }

    if (level === MemberWarningLevel.Level1) {
      return true;
    }

    if (level === MemberWarningLevel.Level2) {
      return this.isWarningChecked(MemberWarningLevel.Level1);
    }

    return this.isWarningChecked(MemberWarningLevel.Level2);
  }

  canClearWarning(level: MemberWarningLevel): boolean {
    if (this.warningsToAssign.has(level)) return true;

    const warning = this.memberWarnings.find(w => w.level === level && this.isWarningActive(w, new Date()));
    if (!warning) return false;

    const currentUserKey = this.authService.getAuthStateValue()?.userKey?.toLowerCase();
    return !!currentUserKey && warning.issuedByUserKey.toLowerCase() === currentUserKey;
  }

  isWarningBusy(level: MemberWarningLevel): boolean {
    void level;
    return false; // we don't have flight status for inline anymore
  }

  onWarningToggle(level: MemberWarningLevel, isChecked: boolean): void {
    this.markFormDirty();
    if (!this.memberKey) return;

    if (isChecked) {
      if (!this.canSelectWarning(level)) return;

      const existing = this.memberWarnings.find(w => w.level === level && this.isWarningActive(w, new Date()));
      if (existing && this.warningsToCancel.has(existing.memberWarningKey)) {
        this.warningsToCancel.delete(existing.memberWarningKey);
      } else {
        this.warningsToAssign.add(level);
      }
    } else {
      if (!this.canClearWarning(level)) return;

      if (this.warningsToAssign.has(level)) {
        this.warningsToAssign.delete(level);
      } else {
        const existing = this.getActiveWarning(level);
        if (existing) {
          this.warningsToCancel.add(existing.memberWarningKey);
        }
      }
    }
  }

  private ensurePlastLevelMap() {
    this.levelsConfig.forEach(config => {
      let rec = this.member.plastLevelHistories.find(x => x.plastLevel === config.level);
      if (!rec) {
        rec = { plastLevel: config.level, dateAchieved: null };
        this.member.plastLevelHistories.push(rec);
      }
      this.plastLevelMap[config.level] = rec;
    });
  }

  private buildPlastLevelsPayload() {
    const history = this.member?.plastLevelHistories ?? [];
    const filteredHistory = history.filter(x => x.dateAchieved != null);
    filteredHistory.forEach(x => x.dateAchieved = toDateOnlyString(x.dateAchieved));
    return filteredHistory;
  }

  private processPendingWarnings(memberKey: string): Observable<MemberWarningDto[] | null> {
    const tasks: Observable<MemberWarningDto>[] = [];

    for (const key of this.warningsToCancel) {
      tasks.push(this.memberWarningService.cancelWarning(memberKey, key));
    }

    const levelsToAssign = Array.from(this.warningsToAssign).sort();
    for (const level of levelsToAssign) {
      tasks.push(this.memberWarningService.assignWarning(memberKey, { level }));
    }

    if (tasks.length === 0) {
      return of(null);
    }

    return from(tasks).pipe(
      concatMap(task => task),
      toArray()
    );
  }

  submit(): void {
    const baseDto: UpsertMemberDto = {
      firstName: this.member.firstName,
      middleName: this.member.middleName,
      lastName: this.member.lastName,
      email: this.member.email,
      phoneNumber: this.member.phoneNumber,
      dateOfBirth: toDateOnlyString(this.member.dateOfBirth)!,
      plastLevelHistories: this.buildPlastLevelsPayload(),
    };

    if (this.groupKey) {
      baseDto.groupKey = this.groupKey;
    } else if (this.kurinKey) {
      baseDto.kurinKey = this.kurinKey;
    }

    const saveObs = this.isCreate 
      ? this.memberService.create({ ...baseDto, createUserAccount: this.createUserAccount }, this.fileToUpload)
      : this.memberService.update(this.memberKey, { ...baseDto, removeProfilePhoto: this.removeProfilePhoto }, this.fileToUpload);

    saveObs.subscribe({
      next: (savedMember) => {
        this.processPendingWarnings(savedMember.memberKey).subscribe({
          next: () => {
            if (this.cameFromMember) {
              this.location.back();
            } else {
              this.router.navigate(['/member', savedMember.memberKey], { replaceUrl: true });
            }
          },
          error: (warnErr) => {
            console.error('Error processing warnings:', warnErr);
            // Still navigate even if warnings failed partially
            if (this.cameFromMember) {
              this.location.back();
            } else {
              this.router.navigate(['/member', savedMember.memberKey], { replaceUrl: true });
            }
          }
        });
      },
      error: (error) => {
        console.error('Error saving member:', error);
      }
    });
  }

  confirmDeleteMember(event: Event) {
      this.confirmationService.confirm({
          target: event.target as EventTarget,
          message: 'Do you want to delete this record?',
          header: 'Danger Zone',
          icon: 'pi pi-info-circle',
          rejectLabel: 'Cancel',
          rejectButtonProps: {
              label: 'Cancel',
              severity: 'secondary',
              outlined: true,
          },
          acceptButtonProps: {
              label: 'Delete',
              severity: 'danger',
          },

          accept: () => {
            this.memberService.delete(this.memberKey).subscribe({
              next: () => {
                this.router.navigate(['/group', this.groupKey]);
              },
              error: (error) => {
                console.error('Error deleting member:', error);
              }
            });
          }
      });
  }

  fileChangeEvent(event: FileSelectEvent): void {
    const file = event.files?.[0];
    if (!file) {
      console.warn('No file selected');
      return;
    }
    if (this.objectUrlToRevoke) {
      URL.revokeObjectURL(this.objectUrlToRevoke);
      this.objectUrlToRevoke = null;
    }
    this.imageFile = file;
    this.croppedImage = '';
    this.croppedFile = null;
    
    this.removeProfilePhoto = false;
    
    this.displayCropper = true;
  }

  imageCropped(event: ImageCroppedEvent) {
    if (event.base64) {
      this.croppedImage = event.base64;
      this.croppedFile = null;
      return;
    }

    if (event.blob) {
      this.croppedFile = new File(
        [event.blob],
        this.imageFile?.name?.replace(/\.[^.]+$/, '.png') || 'profile.png',
        { type: event.blob.type || 'image/png' }
      );

      const url = event.objectUrl || URL.createObjectURL(event.blob);
      if (this.objectUrlToRevoke && this.objectUrlToRevoke !== url) {
        URL.revokeObjectURL(this.objectUrlToRevoke);
      }
      this.objectUrlToRevoke = url;
      this.croppedImage = url;
    } else {
      console.warn('Crop event без base64 і blob', event);
    }
  }

  save(form: FormGroup) {
    this.displayCropper = false;
    this.fileToUpload = this.croppedFile ?? (this.croppedImage ? base64ToBlob(this.croppedImage) : null);

    if (this.fileToUpload) {
      this.removeProfilePhoto = false;
      form.markAsTouched();
    }
    else {
      console.warn('Nothing to upload (cropped image undefined)');
    }
  }

  onCancelCrop() {
    this.displayCropper = false;
    this.croppedImage = '';
  }

  clearProfilePhoto(form: FormGroup) {
    if (this.objectUrlToRevoke) {
      URL.revokeObjectURL(this.objectUrlToRevoke);
      this.objectUrlToRevoke = null;
    }
    this.imageFile = undefined;
    this.croppedImage = '';
    this.croppedFile = null;
    this.fileToUpload = null;
    this.member.profilePhotoUrl = null;

    form.markAsTouched();
    this.removeProfilePhoto = true;
  }
}
