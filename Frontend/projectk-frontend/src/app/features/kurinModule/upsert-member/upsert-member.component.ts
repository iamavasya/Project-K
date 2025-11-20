import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FloatLabelModule } from 'primeng/floatlabel';
import { FormGroup, FormsModule } from '@angular/forms';
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

@Component({
  selector: 'app-upsert-member',
  imports: [FloatLabelModule, FormsModule, InputTextModule, InputMaskModule, DatePickerModule, ButtonModule, ConfirmDialogModule, MinAgeValidatorDirective, FileUploadModule, ImageCropperComponent, DialogModule, AccordionModule, ToggleSwitchModule],
  providers: [ConfirmationService],
  templateUrl: './upsert-member.component.html',
  styleUrl: './upsert-member.component.css'
})
export class UpsertMemberComponent implements OnInit {
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

  route = inject(ActivatedRoute);
  router = inject(Router);
  location = inject(Location);
  memberService = inject(MemberService);
  confirmationService = inject(ConfirmationService);

  private cameFromMember = false;

  isCreate = false;

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
      this.memberKey = params.get('memberKey')!;
      this.groupKey = params.get('groupKey')!;
    });
    if (this.memberKey) {
      this.loadData();
      this.isCreate = false;
    } else {
      this.ensurePlastLevelMap();
      this.isCreate = true;
    }
    const navState = (this.router.getCurrentNavigation()?.extras.state as { fromMember?: boolean } | undefined) ?? history.state;
    this.cameFromMember = navState?.fromMember === true;
  }

  private loadData(): void {
    this.memberService.getByKey(this.memberKey).subscribe({
      next: (member) => {
        console.log('Fetched member:', member);
        this.member = member;
        if (!this.member.plastLevelHistories) this.member.plastLevelHistories = [];
        this.ensurePlastLevelMap();
        console.log(member);
      },
      error: (error) => {
        console.error('Error fetching member:', error);
        this.router.navigate(['/group', this.groupKey], { replaceUrl: true });
      }
    });
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

  submit(): void {
    const baseDto: UpsertMemberDto = {
      groupKey: this.groupKey,
      firstName: this.member.firstName,
      middleName: this.member.middleName,
      lastName: this.member.lastName,
      email: this.member.email,
      phoneNumber: this.member.phoneNumber,
      dateOfBirth: toDateOnlyString(this.member.dateOfBirth)!,
      plastLevelHistories: this.buildPlastLevelsPayload(),
    };

    if (this.isCreate) {
      this.memberService.create(baseDto, this.fileToUpload).subscribe({
        next: (createdMember) => {
          console.log('Member created:', createdMember);
          this.router.navigate(['/member', createdMember.memberKey], { replaceUrl: true });
        },
        error: (error) => {
          console.error('Error creating member:', error, baseDto);
        }
      });
    } else {
      baseDto.removeProfilePhoto = this.removeProfilePhoto;

      console.log(baseDto.plastLevelHistories);
      this.memberService.update(this.memberKey, baseDto, this.fileToUpload).subscribe({
        next: (updatedMember) => {
          console.log('Member updated:', updatedMember);
          if (this.cameFromMember) {
            this.location.back();
            return;
          }
          this.router.navigate(['/member', updatedMember.memberKey], { replaceUrl: true });
        },
        error: (error) => {
          console.error('Error updating member:', error);
        }
      });
    }
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
    console.log('Clearing profile photo');
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
