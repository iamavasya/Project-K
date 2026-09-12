import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { ProfileVerificationBadgeComponent } from './profile-verification-badge';
import { MemberProfileVerificationStatus } from '../../models/enums/member-profile-verification-status.enum';

describe('ProfileVerificationBadgeComponent', () => {
  let fixture: ComponentFixture<ProfileVerificationBadgeComponent>;
  let component: ProfileVerificationBadgeComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProfileVerificationBadgeComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(ProfileVerificationBadgeComponent);
    component = fixture.componentInstance;
  });

  it('should show current verification badge', () => {
    fixture.componentRef.setInput('status', MemberProfileVerificationStatus.VerifiedCurrent);
    fixture.detectChanges();

    const badge = fixture.debugElement.query(By.css('.profile-verification-badge'));

    expect(badge).not.toBeNull();
    expect(badge.nativeElement.classList).toContain('profile-verification-badge--current');
    expect(component.tooltip).toBe('Дані верифіковано');
  });

  it('should show stale verification badge', () => {
    fixture.componentRef.setInput('status', MemberProfileVerificationStatus.VerifiedStale);
    fixture.detectChanges();

    const badge = fixture.debugElement.query(By.css('.profile-verification-badge'));

    expect(badge).not.toBeNull();
    expect(badge.nativeElement.classList).toContain('profile-verification-badge--stale');
    expect(component.tooltip).toBe('Дані змінено після верифікації');
  });

  it('should hide badge when disabled or unverified', () => {
    fixture.componentRef.setInput('status', MemberProfileVerificationStatus.VerifiedCurrent);
    fixture.componentRef.setInput('enabled', false);
    fixture.detectChanges();

    expect(fixture.debugElement.query(By.css('.profile-verification-badge'))).toBeNull();

    fixture.componentRef.setInput('enabled', true);
    fixture.componentRef.setInput('status', MemberProfileVerificationStatus.Unverified);
    fixture.detectChanges();

    expect(fixture.debugElement.query(By.css('.profile-verification-badge'))).toBeNull();
  });
});
