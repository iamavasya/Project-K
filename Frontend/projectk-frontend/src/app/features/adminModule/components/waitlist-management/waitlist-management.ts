import { Component, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';

import { OnboardingService, WaitlistEntry, ZbtStats } from '../../../authModule/services/onboarding.service';
import { TableModule } from '@openng/optimus-ui/table';
import { ButtonModule } from '@openng/optimus-ui/button';
import { TagModule } from '@openng/optimus-ui/tag';
import { TooltipModule } from '@openng/optimus-ui/tooltip';
import { MessageService, ConfirmationService } from '@openng/optimus-ui/api';
import { ToastModule } from '@openng/optimus-ui/toast';
import { ProgressBarModule } from '@openng/optimus-ui/progressbar';
import { ConfirmDialogModule } from '@openng/optimus-ui/confirmdialog';
import { DialogModule } from '@openng/optimus-ui/dialog';
import { TextareaModule } from '@openng/optimus-ui/textarea';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../../features/authModule/services/authService/auth.service';
import {
  getWaitlistStatusLabel,
  getWaitlistStatusSeverity,
  isWaitlistApproved,
  isWaitlistInitial
} from '../../common/functions/waitlist-status.function';
import { LocalUtcDatePipe } from '../../../../shared/pipes/local-utc-date.pipe';
import { EmptyStateComponent } from '../../../../shared/empty-state/empty-state';

@Component({
  selector: 'app-waitlist-management',
  imports: [
    TableModule,
    ButtonModule,
    TagModule,
    TooltipModule,
    ToastModule,
    ProgressBarModule,
    ConfirmDialogModule,
    DialogModule,
    TextareaModule,
    FormsModule,
    LocalUtcDatePipe,
    EmptyStateComponent
],
  providers: [MessageService, ConfirmationService],
  template: `
    <p-toast />
    <p-confirmDialog />

    <p-dialog [(visible)]="rejectionDialogVisible" header="Відхилити заявку" [modal]="true" [style]="{width: '450px'}">
        <div class="flex flex-col gap-4">
            <p>Відхилити заявку <strong>{{ selectedEntry?.firstName }} {{ selectedEntry?.lastName }}</strong>?</p>
            <div class="flex flex-col gap-2">
                <label for="note">Причина відмови (не обовʼязково)</label>
                <textarea id="note" pTextarea [(ngModel)]="rejectionNote" rows="3" class="w-full"></textarea>
            </div>
        </div>
        <ng-template pTemplate="footer">
            <p-button label="Скасувати" icon="pi pi-times" text (onClick)="rejectionDialogVisible = false" />
            <p-button label="Відхилити" icon="pi pi-check" severity="danger" (onClick)="confirmReject()" />
        </ng-template>
    </p-dialog>

    <div class="card p-4">
      <div class="flex justify-between items-center mb-6">
        <h2 class="text-2xl font-bold">Заявки на приєднання</h2>
        
        @if (stats && stats.isClosedBeta) {
          <div class="flex flex-col items-end gap-1">
            <div class="flex items-center gap-3">
              <span class="text-sm font-semibold text-gray-600">
                @if (stats.scope === 'Kurin') {
                    Ліміт ЗБТ ({{ stats.kurinName }})
                } @else {
                    Ліміт ЗБТ (глобальний)
                }
              </span>
              <p-tag [severity]="stats.isCapReached ? 'danger' : 'info'"
                     [value]="stats.currentActiveUsers + ' / ' + stats.betaCap" />
            </div>
            <p-progressBar [value]="(stats.currentActiveUsers / stats.betaCap) * 100" 
                           [showValue]="false" 
                           class="w-64 h-2"
                           [color]="stats.isCapReached ? '#ef4444' : '#3b82f6'" />
          </div>
        }
      </div>

      <p-table [value]="entries" [responsiveLayout]="'scroll'" [loading]="loading" styleClass="p-datatable-sm">
        <ng-template pTemplate="header">
          <tr>
            <th>Імʼя</th>
            <th>Email</th>
            <th>Станиця</th>
            <th>Край</th>
            <th>Претендує на курінь</th>
            <th>Статус</th>
            <th>Подано</th>
            <th style="width: 120px">Дії</th>
          </tr>
        </ng-template>
        <ng-template pTemplate="body" let-entry>
          <tr>
            <td>{{ entry.firstName }} {{ entry.lastName }}</td>
            <td>{{ entry.email }}</td>
            <td>{{ entry.stanytsia || '-' }}</td>
            <td>{{ entry.regionOrCountry || '-' }}</td>
            <td>
              @if (entry.isKurinLeaderCandidate) {
                <p-tag severity="info" [value]="'Курінь ' + entry.claimedKurinNameOrNumber" />
              } @else {
                <span class="text-gray-400 text-sm italic">Звичайний учасник</span>
              }
            </td>
            <td>
              <p-tag [severity]="getStatusSeverity(entry.verificationStatus)" [value]="getStatusLabel(entry.verificationStatus)" />
            </td>
            <td>{{ entry.requestedAtUtc | localUtcDate:'short' }}</td>
            <td>
              <div class="flex gap-2">
                @if (isInitial(entry.verificationStatus)) {
                  <p-button icon="pi pi-check" severity="success" rounded text
                            (onClick)="approve(entry)" pTooltip="Схвалити й надіслати запрошення" />
                  <p-button icon="pi pi-times" severity="danger" rounded text
                            (onClick)="reject(entry)" pTooltip="Відхилити" />
                }
                @if (isApproved(entry.verificationStatus)) {
                  <p-button icon="pi pi-refresh" severity="secondary" rounded text
                            (onClick)="resend(entry)" pTooltip="Надіслати запрошення ще раз" />
                }
              </div>
            </td>
          </tr>
        </ng-template>
        <ng-template pTemplate="emptymessage">
            <tr>
                <td colspan="8" class="lil-empty-cell">
                    <app-empty-state
                        art="list"
                        title="Заявок немає"
                        body="Нові заявки на приєднання зʼявляться тут." />
                </td>
            </tr>
        </ng-template>
      </p-table>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`
    .waitlist-empty-cell {
      border: 0 !important;
      padding: 1.25rem 0 0 !important;
    }
  `]
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
        this.messageService.add({ severity: 'error', summary: 'Помилка', detail: 'Не вдалося завантажити заявки' });
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
            message: `Ліміт бети (${this.stats.betaCap}) вичерпано. Схвалення цього користувача перевищить його. Продовжити?`,
            header: 'Ліміт бети досягнуто',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => this.executeApproval(entry)
        });
    } else {
        this.confirmationService.confirm({
            message: `Схвалити заявку ${entry.firstName} ${entry.lastName}? На ${entry.email} піде лист із запрошенням.`,
            header: 'Підтвердити схвалення',
            icon: 'pi pi-user-plus',
            accept: () => this.executeApproval(entry)
        });
    }
  }

  private executeApproval(entry: WaitlistEntry) {
    this.onboardingService.approveWaitlistEntry(entry.waitlistEntryKey).subscribe({
        next: () => {
          this.messageService.add({ severity: 'success', summary: 'Схвалено', detail: 'Запрошення надіслано' });
          this.loadEntries();
          this.loadStats();
        },
        error: (err) => {
          this.messageService.add({ severity: 'error', summary: 'Помилка', detail: err.error?.message || 'Не вдалося схвалити' });
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
        this.messageService.add({ severity: 'info', summary: 'Відхилено', detail: 'Заявку відхилено' });
        this.rejectionDialogVisible = false;
        this.loadEntries();
        this.loadStats();
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: 'Помилка', detail: 'Не вдалося відхилити' });
      }
    });
  }

  resend(entry: WaitlistEntry) {
    this.confirmationService.confirm({
        message: `Надіслати запрошення на ${entry.email} ще раз?`,
        header: 'Надіслати ще раз?',
        icon: 'pi pi-refresh',
        accept: () => {
            this.onboardingService.resendInvitation(entry.waitlistEntryKey).subscribe({
                next: () => {
                  this.messageService.add({ severity: 'success', summary: 'Надіслано', detail: 'Запрошення надіслано ще раз' });
                },
                error: () => {
                  this.messageService.add({ severity: 'error', summary: 'Помилка', detail: 'Не вдалося надіслати' });
                }
              });
        }
    });
  }

  getStatusLabel(status: string | number): string {
    return getWaitlistStatusLabel(status);
  }

  getStatusSeverity(status: string | number): "success" | "info" | "warn" | "danger" | "secondary" | "contrast" | undefined {
    return getWaitlistStatusSeverity(status);
  }

  isInitial(status: string | number): boolean {
    return isWaitlistInitial(status);
  }

  isApproved(status: string | number): boolean {
    return isWaitlistApproved(status);
  }
}
