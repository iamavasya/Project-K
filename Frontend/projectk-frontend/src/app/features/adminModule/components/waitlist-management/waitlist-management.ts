import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { OnboardingService, WaitlistEntry, ZbtStats } from '../../../authModule/services/onboarding.service';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService, ConfirmationService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { ProgressBarModule } from 'primeng/progressbar';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { TextareaModule } from 'primeng/textarea';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../../features/authModule/services/authService/auth.service';

@Component({
  selector: 'app-waitlist-management',
  standalone: true,
  imports: [
    CommonModule, 
    TableModule, 
    ButtonModule, 
    TagModule, 
    TooltipModule, 
    ToastModule, 
    ProgressBarModule, 
    ConfirmDialogModule, 
    DialogModule, 
    TextareaModule, 
    FormsModule
  ],
  providers: [MessageService, ConfirmationService],
  template: `
    <p-toast></p-toast>
    <p-confirmDialog></p-confirmDialog>

    <p-dialog [(visible)]="rejectionDialogVisible" header="Reject Applicant" [modal]="true" [style]="{width: '450px'}">
        <div class="flex flex-col gap-4">
            <p>Are you sure you want to reject <strong>{{ selectedEntry?.firstName }} {{ selectedEntry?.lastName }}</strong>?</p>
            <div class="flex flex-col gap-2">
                <label for="note">Rejection Note (optional)</label>
                <textarea id="note" pTextarea [(ngModel)]="rejectionNote" rows="3" class="w-full"></textarea>
            </div>
        </div>
        <ng-template pTemplate="footer">
            <p-button label="Cancel" icon="pi pi-times" text (onClick)="rejectionDialogVisible = false"></p-button>
            <p-button label="Reject" icon="pi pi-check" severity="danger" (onClick)="confirmReject()"></p-button>
        </ng-template>
    </p-dialog>

    <div class="card p-4">
      <div class="flex justify-between items-center mb-6">
        <h2 class="text-2xl font-bold">Waitlist Management</h2>
        
        @if (stats) {
          <div class="flex flex-col items-end gap-1">
            <div class="flex items-center gap-3">
              <span class="text-sm font-semibold text-gray-600">
                @if (stats.scope === 'Kurin') {
                    ZBT CAP ({{ stats.kurinName }})
                } @else {
                    ZBT BETA CAP (Global)
                }
              </span>
              <p-tag [severity]="stats.isCapReached ? 'danger' : 'info'" 
                     [value]="stats.currentActiveUsers + ' / ' + stats.betaCap + ' users'"></p-tag>
            </div>
            <p-progressBar [value]="(stats.currentActiveUsers / stats.betaCap) * 100" 
                           [showValue]="false" 
                           class="w-64 h-2"
                           [color]="stats.isCapReached ? '#ef4444' : '#3b82f6'"></p-progressBar>
          </div>
        }
      </div>

      <p-table [value]="entries" [responsiveLayout]="'scroll'" [loading]="loading" styleClass="p-datatable-sm">
        <ng-template pTemplate="header">
          <tr>
            <th>Name</th>
            <th>Email</th>
            <th>Kurin Candidate</th>
            <th>Status</th>
            <th>Requested At</th>
            <th style="width: 120px">Actions</th>
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
                <span class="text-gray-400 text-sm italic">Standard Member</span>
              }
            </td>
            <td>
              <p-tag [severity]="getStatusSeverity(entry.verificationStatus)" [value]="getStatusLabel(entry.verificationStatus)"></p-tag>
            </td>
            <td>{{ entry.requestedAtUtc | date:'short' }}</td>
            <td>
              <div class="flex gap-2">
                @if (isInitial(entry.verificationStatus)) {
                  <p-button icon="pi pi-check" severity="success" rounded text
                            (onClick)="approve(entry)" pTooltip="Approve & Send Invitation"></p-button>
                  <p-button icon="pi pi-times" severity="danger" rounded text
                            (onClick)="reject(entry)" pTooltip="Reject"></p-button>
                }
                @if (isApproved(entry.verificationStatus)) {
                  <p-button icon="pi pi-refresh" severity="info" rounded text
                            (onClick)="resend(entry)" pTooltip="Resend Invitation"></p-button>
                }
              </div>
            </td>
          </tr>
        </ng-template>
        <ng-template pTemplate="emptymessage">
            <tr>
                <td colspan="6" class="text-center p-4 text-gray-500">No waitlist entries found.</td>
            </tr>
        </ng-template>
      </p-table>
    </div>
  `
})
export class WaitlistManagementComponent implements OnInit {
  entries: WaitlistEntry[] = [];
  stats: ZbtStats | null = null;
  loading = true;

  rejectionDialogVisible = false;
  rejectionNote = '';
  selectedEntry: WaitlistEntry | null = null;

  private onboardingService = inject(OnboardingService);
  private messageService = inject(MessageService);
  private confirmationService = inject(ConfirmationService);
  private authService = inject(AuthService);

  ngOnInit() {
    this.loadEntries();
    this.loadStats();
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

  loadStats() {
    const kurinKey = this.authService.getAuthStateValue()?.kurinKey;
    this.onboardingService.getOnboardingStats(kurinKey || undefined).subscribe({
      next: (data) => {
        this.stats = data;
      }
    });
  }

  approve(entry: WaitlistEntry) {
    if (this.stats?.isCapReached) {
        this.confirmationService.confirm({
            message: `The Beta Cap (${this.stats.betaCap}) has been reached. Approving this user will exceed the target limit. Do you want to proceed anyway?`,
            header: 'Beta Cap Reached',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => this.executeApproval(entry)
        });
    } else {
        this.confirmationService.confirm({
            message: `Approve invitation for ${entry.firstName} ${entry.lastName}? An email will be sent to ${entry.email}.`,
            header: 'Confirm Approval',
            icon: 'pi pi-user-plus',
            accept: () => this.executeApproval(entry)
        });
    }
  }

  private executeApproval(entry: WaitlistEntry) {
    this.onboardingService.approveWaitlistEntry(entry.waitlistEntryKey).subscribe({
        next: () => {
          this.messageService.add({ severity: 'success', summary: 'Approved', detail: 'Invitation sent' });
          this.loadEntries();
          this.loadStats();
        },
        error: (err) => {
          this.messageService.add({ severity: 'error', summary: 'Error', detail: err.error?.message || 'Approval failed' });
        }
      });
  }

  reject(entry: WaitlistEntry) {
    this.selectedEntry = entry;
    this.rejectionNote = '';
    this.rejectionDialogVisible = true;
  }

  confirmReject() {
    if (!this.selectedEntry) return;

    this.onboardingService.rejectWaitlistEntry(this.selectedEntry.waitlistEntryKey, this.rejectionNote).subscribe({
      next: () => {
        this.messageService.add({ severity: 'info', summary: 'Rejected', detail: 'Applicant rejected' });
        this.rejectionDialogVisible = false;
        this.loadEntries();
        this.loadStats();
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Rejection failed' });
      }
    });
  }

  resend(entry: WaitlistEntry) {
    this.confirmationService.confirm({
        message: `Resend invitation to ${entry.email}?`,
        header: 'Confirm Resend',
        icon: 'pi pi-refresh',
        accept: () => {
            this.onboardingService.resendInvitation(entry.waitlistEntryKey).subscribe({
                next: () => {
                  this.messageService.add({ severity: 'success', summary: 'Sent', detail: 'Invitation resent' });
                },
                error: () => {
                  this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to resend' });
                }
              });
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
