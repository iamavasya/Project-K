import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { BehaviorSubject, of } from 'rxjs';
import { NotificationService } from '../../services/notification.service';
import { NotificationBell } from './notification-bell';

describe('NotificationBell', () => {
  let component: NotificationBell;
  let fixture: ComponentFixture<NotificationBell>;
  let notificationService: jasmine.SpyObj<NotificationService>;

  beforeEach(async () => {
    notificationService = jasmine.createSpyObj<NotificationService>(
      'NotificationService',
      ['getInbox', 'refreshUnreadCount', 'markAllAsRead', 'markAsRead', 'setUnreadCount'],
      { unreadCount$: new BehaviorSubject(0) }
    );
    notificationService.getInbox.and.returnValue(of([]));
    notificationService.refreshUnreadCount.and.returnValue(of(0));

    await TestBed.configureTestingModule({
      imports: [NotificationBell],
      providers: [
        { provide: NotificationService, useValue: notificationService },
        { provide: Router, useValue: jasmine.createSpyObj<Router>('Router', ['navigateByUrl']) }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NotificationBell);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('renders the inbox controls in Ukrainian', () => {
    component.isOpen = true;
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Сповіщення');
    expect(fixture.nativeElement.textContent).toContain('Сповіщень немає');
    expect(fixture.nativeElement.querySelector('[aria-label="Позначити всі як прочитані"]')).not.toBeNull();
  });

  it('closes the inbox when clicking outside the component', () => {
    component.isOpen = true;

    document.body.dispatchEvent(new MouseEvent('click', { bubbles: true }));

    expect(component.isOpen).toBeFalse();
  });

  it('keeps the inbox open when clicking inside the component', () => {
    component.isOpen = true;

    fixture.nativeElement.dispatchEvent(new MouseEvent('click', { bubbles: true }));

    expect(component.isOpen).toBeTrue();
  });
});
