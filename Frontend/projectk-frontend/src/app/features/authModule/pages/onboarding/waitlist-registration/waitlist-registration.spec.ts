import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { WaitlistRegistrationComponent } from './waitlist-registration';
import { OnboardingService } from '../../../services/onboarding-service/onboarding.service';

describe('WaitlistRegistrationComponent', () => {
  let fixture: ComponentFixture<WaitlistRegistrationComponent>;
  let component: WaitlistRegistrationComponent;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [WaitlistRegistrationComponent],
      providers: [
        provideRouter([]),
        { provide: OnboardingService, useValue: { submitWaitlist: () => of(undefined) } }
      ]
    });
    fixture = TestBed.createComponent(WaitlistRegistrationComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  function element(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  function fillEverythingBut(leader: boolean): void {
    component.form.patchValue({
      firstName: 'Марта', lastName: 'Сова', email: 'marta@example.com', phoneNumber: '+380 50 123 45 67',
      dateOfBirth: new Date(1990, 0, 1), stanytsia: 'Тернопіль', regionOrCountry: 'Україна',
      isKurinLeaderCandidate: leader, claimedKurinNameOrNumber: '12'
    });
    fixture.detectChanges();
  }

  it('keeps the button disabled and explains itself while the applicant is not a Зв’язковий', () => {
    fillEverythingBut(false);

    expect(component.form.invalid).toBeTrue();
    expect(element().querySelector('button[type=submit]')?.hasAttribute('disabled')).toBeTrue();
    expect(element().textContent).toContain('приймаємо лише курені');
  });

  it('lets a Зв’язковий with a kurin number apply, and drops the notice', () => {
    fillEverythingBut(true);

    expect(component.form.valid).toBeTrue();
    expect(element().querySelector('button[type=submit]')?.hasAttribute('disabled')).toBeFalse();
    expect(element().textContent).not.toContain('приймаємо лише курені');
  });
});
