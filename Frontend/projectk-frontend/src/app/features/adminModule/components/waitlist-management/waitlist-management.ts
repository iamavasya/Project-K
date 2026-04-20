import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { OnboardingService, WaitlistEntry } from '../../../authModule/services/onboarding.service';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';

@Component({
  selector: 'app-waitlist-management',
  standalone: true,
  imports: [CommonModule, TableModule, ButtonModule, TagModule, TooltipModule, ToastModule],
  providers: [MessageService],
  template: `
    <p-toast></p-toast>
    <div class="card p-4">
      <h2 class="text-2xl font-bold mb-4">Waitlist Management</h2>
      <p-table [value]="entries" [responsiveLayout]="'scroll'" [loading]="loading">
        <ng-template pTemplate="header">
          <tr>
            <th>Name</th>
            <th>Email</th>
            <th>Kurin Candidate</th>
            <th>Status</th>
            <th>Requested At</th>
            <th>Actions</th>
          </tr>
        </ng-template>
        <ng-template pTemplate="body" let-entry>
          <tr>
            <td>{{ entry.firstName }} {{ entry.lastName }}</td>
            <td>{{ entry.email }}</td>
            <td>
              @if (entry.isKurinLeaderCandidate) {
                <p-tag severity="info" [value]="'Kurin ' + entry.claimedKurinNameOrNumber"></p-tag>
              } @else {
                <span>No</span>
              }
            </td>
            <td>
              <p-tag [severity]="getStatusSeverity(entry.verificationStatus)" [value]="getStatusLabel(entry.verificationStatus)"></p-tag>
            </td>
            <td>{{ entry.requestedAtUtc | date:'short' }}</td>
            <td>
              <div class="flex gap-2">
                @if (isInitial(entry.verificationStatus)) {
                  <p-button icon="pi pi-check" severity="success" 
                            (onClick)="approve(entry)" pTooltip="Approve & Send Invitation"></p-button>
                  <p-button icon="pi pi-times" severity="danger" 
                            (onClick)="reject(entry)" pTooltip="Reject"></p-button>
                }
                @if (isApproved(entry.verificationStatus)) {
                  <p-button icon="pi pi-refresh" severity="info" 
                            (onClick)="resend(entry)" pTooltip="Resend Invitation"></p-button>
                }
              </div>
            </td>
          </tr>
        </ng-template>
      </p-table>
    </div>
  `
})
export class WaitlistManagementComponent implements OnInit {
  entries: WaitlistEntry[] = [];
  loading = true;

  private onboardingService = inject(OnboardingService);
  private messageService = inject(MessageService);

  ngOnInit() {
    this.loadEntries();
  }

  loadEntries() {
    this.loading = true;
    this.onboardingService.getWaitlistEntries().subscribe({
      next: (data) => {
        this.entries = data;
        this.loading = false;
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to load waitlist entries' });
        this.loading = false;
      }
    });
  }

  approve(entry: WaitlistEntry) {
    this.onboardingService.approveWaitlistEntry(entry.waitlistEntryKey).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Approved', detail: 'Invitation sent' });
        this.loadEntries();
      },
      error: (err) => {
        this.messageService.add({ severity: 'error', summary: 'Error', detail: err.error?.message || 'Approval failed' });
      }
    });
  }

  reject(entry: WaitlistEntry) {
    // For now, no note
    this.onboardingService.rejectWaitlistEntry(entry.waitlistEntryKey).subscribe({
      next: () => {
        this.messageService.add({ severity: 'info', summary: 'Rejected', detail: 'Applicant rejected' });
        this.loadEntries();
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Rejection failed' });
      }
    });
  }

  resend(entry: WaitlistEntry) {
    this.onboardingService.resendInvitation(entry.waitlistEntryKey).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Sent', detail: 'Invitation resent' });
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to resend' });
      }
    });
  }

  getStatusLabel(status: string | number): string {
    const s = String(status);
    switch (s) {
      case '0':
      case 'Submitted': return 'Submitted';
      case '1':
      case 'NeedsManualVerification': return 'Verification Required';
      case '2':
      case 'Verified': return 'Verified';
      case '3':
      case 'Rejected': return 'Rejected';
      case '4':
      case 'ApprovedForInvitation': return 'Approved';
      default: return 'Unknown (' + s + ')';
    }
  }

  getStatusSeverity(status: string | number): "success" | "info" | "warn" | "danger" | "secondary" | "contrast" | undefined {
    const s = String(status);
    switch (s) {
      case '0':
      case 'Submitted': return 'info';
      case '1':
      case 'NeedsManualVerification': return 'warn';
      case '2':
      case 'Verified': return 'success';
      case '3':
      case 'danger':
      case 'Rejected': return 'danger';
      case '4':
      case 'ApprovedForInvitation': return 'success';
      default: return 'secondary';
    }
  }

  isInitial(status: string | number): boolean {
    const s = String(status);
    return s === '0' || s === 'Submitted';
  }

  isApproved(status: string | number): boolean {
    const s = String(status);
    return s === '4' || s === 'ApprovedForInvitation';
  }
}
