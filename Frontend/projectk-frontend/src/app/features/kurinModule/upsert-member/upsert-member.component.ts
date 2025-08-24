import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FloatLabelModule } from 'primeng/floatlabel';
import { FormsModule } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { InputMaskModule } from 'primeng/inputmask';
import { DatePickerModule } from 'primeng/datepicker';
import { MemberService } from '../common/services/member-service/member.service';
import { ButtonModule } from 'primeng/button';
import { eventMarker } from '@primeuix/themes/aura/timeline';
import { MemberDto } from '../common/models/memberDto';
import { UpsertMemberDto } from '../common/models/requests/member/upsertMemberDto';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';

@Component({
  selector: 'app-upsert-member',
  imports: [FloatLabelModule, FormsModule, InputTextModule, InputMaskModule, DatePickerModule, ButtonModule, ConfirmDialogModule],
  providers: [ConfirmationService],
  templateUrl: './upsert-member.component.html',
  styleUrl: './upsert-member.component.scss'
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
    dateOfBirth: new Date(),
  };

  memberKey: string = '';
  groupKey: string = '';
  route = inject(ActivatedRoute);
  router = inject(Router);
  memberService = inject(MemberService);
  confirmationService = inject(ConfirmationService);
  isCreate: boolean = false;

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.memberKey = params.get('memberKey')!;
      this.groupKey = params.get('groupKey')!;
    });
    if (this.memberKey) {
      this.loadData();
      this.isCreate = false;
    } else {
      this.isCreate = true;
    }
  }

  private loadData(): void {
    this.memberService.getByKey(this.memberKey).subscribe({
      next: (member) => {
        this.member = member;
        console.log(member);
      },
      error: (error) => {
        console.error('Error fetching member:', error);
      }
    });
  }

  submit(): void {
    if (this.isCreate) {
      const memberDto: UpsertMemberDto = {
        groupKey: this.groupKey,
        firstName: this.member.firstName,
        middleName: this.member.middleName,
        lastName: this.member.lastName,
        email: this.member.email,
        phoneNumber: this.member.phoneNumber,
        dateOfBirth: this.toDateOnlyString(this.member.dateOfBirth),
      };
      this.memberService.create(memberDto).subscribe({
        next: (createdMember) => {
          console.log('Member created:', createdMember);
          this.router.navigate(['/member', createdMember.memberKey]);
        },
        error: (error) => {
          console.error('Error creating member:', error, memberDto);
        }
      });
    } else {
      const memberDto: UpsertMemberDto = {
        groupKey: this.member.groupKey,
        firstName: this.member.firstName,
        middleName: this.member.middleName,
        lastName: this.member.lastName,
        email: this.member.email,
        phoneNumber: this.member.phoneNumber,
        dateOfBirth: this.toDateOnlyString(this.member.dateOfBirth),
      };
      this.memberService.update(this.memberKey, memberDto).subscribe({
        next: (updatedMember) => {
          console.log('Member updated:', updatedMember);
          this.router.navigate(['/group', this.groupKey]);
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

  private toDateOnlyString(d: Date | string | null | undefined): string {
    if (!d) return '';
    if (typeof d === 'string') {
      // якщо вже yyyy-MM-dd
      if (/^\d{4}-\d{2}-\d{2}$/.test(d)) return d;
      // якщо ISO datetime
      const only = d.split('T')[0];
      return only;
    }
    const y = d.getFullYear();
    const m = (d.getMonth() + 1).toString().padStart(2, '0');
    const day = d.getDate().toString().padStart(2, '0');
    return `${y}-${m}-${day}`;
  }
}
