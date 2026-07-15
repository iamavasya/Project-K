import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { NotificationService } from './notification.service';
import { AppNotification } from '../models/app-notification.model';

describe('NotificationService', () => {
  let service: NotificationService;
  let httpMock: HttpTestingController;
  const apiUrl = `${environment.apiUrl}/notifications`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });

    service = TestBed.inject(NotificationService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should load inbox with query params', () => {
    const response = [makeNotification()];

    service.getInbox(true, 5).subscribe(result => {
      expect(result).toEqual(response);
    });

    const req = httpMock.expectOne(request =>
      request.url === apiUrl
      && request.params.get('unreadOnly') === 'true'
      && request.params.get('take') === '5'
    );
    expect(req.request.method).toBe('GET');
    req.flush(response);
  });

  it('should refresh unread count state', () => {
    const observed: number[] = [];
    service.unreadCount$.subscribe(count => observed.push(count));

    service.refreshUnreadCount().subscribe(count => {
      expect(count).toBe(3);
    });

    const req = httpMock.expectOne(`${apiUrl}/unread-count`);
    expect(req.request.method).toBe('GET');
    req.flush(3);

    expect(observed).toEqual([0, 3]);
  });

  it('should mark notification as read', () => {
    const notification = { ...makeNotification(), isRead: true, readAtUtc: '2026-06-16T12:00:00Z' };

    service.markAsRead(notification.notificationKey).subscribe(result => {
      expect(result).toEqual(notification);
    });

    const req = httpMock.expectOne(`${apiUrl}/${notification.notificationKey}/read`);
    expect(req.request.method).toBe('PUT');
    req.flush(notification);
  });

  it('should mark all as read and clear unread count', () => {
    const observed: number[] = [];
    service.setUnreadCount(4);
    service.unreadCount$.subscribe(count => observed.push(count));

    service.markAllAsRead().subscribe(count => {
      expect(count).toBe(4);
    });

    const req = httpMock.expectOne(`${apiUrl}/read-all`);
    expect(req.request.method).toBe('PUT');
    req.flush(4);

    expect(observed).toEqual([4, 0]);
  });

  function makeNotification(): AppNotification {
    return {
      notificationKey: 'notification-1',
      recipientUserKey: 'user-1',
      type: 'MemberProfileVerified',
      severity: 'Success',
      title: 'Профільні дані підтверджено',
      body: 'Ваші профільні дані підтверджено як актуальні.',
      entityType: 'Member',
      entityKey: 'member-1',
      route: '/member/member-1',
      payloadJson: null,
      createdAtUtc: '2026-06-16T12:00:00Z',
      readAtUtc: null,
      actorUserKey: 'actor-1',
      deduplicationKey: 'member-profile-verified:member-1',
      expiresAtUtc: null,
      isRead: false
    };
  }
});
