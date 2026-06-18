import { Component, DestroyRef, ElementRef, HostListener, inject, OnInit } from '@angular/core';
import { DatePipe, NgClass } from '@angular/common';
import { Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ButtonModule } from 'primeng/button';
import { NotificationService } from '../../services/notification.service';
import { AppNotification, AppNotificationSeverity } from '../../models/app-notification.model';

@Component({
  selector: 'app-notification-bell',
  imports: [ButtonModule, DatePipe, NgClass],
  templateUrl: './notification-bell.html',
  styleUrl: './notification-bell.css'
})
export class NotificationBell implements OnInit {
  private readonly notificationService = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly elementRef = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly router = inject(Router);

  notifications: AppNotification[] = [];
  unreadCount = 0;
  isOpen = false;
  isLoading = false;

  ngOnInit(): void {
    this.notificationService.unreadCount$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(count => {
        this.unreadCount = count;
      });

    this.refreshUnreadCount();
  }

  toggle(): void {
    this.isOpen = !this.isOpen;
    if (this.isOpen) {
      this.loadInbox();
    }
  }

  @HostListener('document:click', ['$event'])
  closeOnOutsideClick(event: MouseEvent): void {
    if (this.isOpen && !this.elementRef.nativeElement.contains(event.target as Node | null)) {
      this.isOpen = false;
    }
  }

  loadInbox(): void {
    this.isLoading = true;
    this.notificationService.getInbox(false, 10)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: notifications => {
          this.notifications = notifications;
          this.isLoading = false;
        },
        error: () => {
          this.notifications = [];
          this.isLoading = false;
        }
      });
  }

  markAllAsRead(): void {
    if (!this.notifications.some(notification => !notification.isRead)) {
      return;
    }

    this.notificationService.markAllAsRead()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        const readAtUtc = new Date().toISOString();
        this.notifications = this.notifications.map(notification => ({
          ...notification,
          isRead: true,
          readAtUtc: notification.readAtUtc ?? readAtUtc
        }));
      });
  }

  openNotification(notification: AppNotification): void {
    const route = notification.route;
    const navigate = (): void => {
      this.isOpen = false;
      if (route) {
        this.router.navigateByUrl(route);
      }
    };

    if (notification.isRead) {
      navigate();
      return;
    }

    this.notificationService.markAsRead(notification.notificationKey)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: updated => {
          this.notifications = this.notifications.map(item =>
            item.notificationKey === updated.notificationKey ? updated : item
          );
          this.notificationService.setUnreadCount(this.unreadCount - 1);
          navigate();
        },
        error: () => navigate()
      });
  }

  severityClass(severity: AppNotificationSeverity): string {
    return `notification-row--${severity.toLowerCase()}`;
  }

  private refreshUnreadCount(): void {
    this.notificationService.refreshUnreadCount()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ error: () => this.notificationService.setUnreadCount(0) });
  }
}
