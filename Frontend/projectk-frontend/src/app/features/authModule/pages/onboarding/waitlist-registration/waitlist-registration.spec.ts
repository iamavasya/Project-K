import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { MessageService } from '@openng/optimus-ui/api';
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
        MessageService,
        {
          provide: OnboardingService,
          useValue: { submitWaitlist: () => of(undefined), getKurinNumberRange: () => of({ min: 1, max: 200 }) }
        }
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

  it('refuses a kurin number outside the range, so nobody takes kurin 0 or 1153', () => {
    fillEverythingBut(true);

    for (const number of ['0', '201', '1153']) {
      component.form.patchValue({ claimedKurinNameOrNumber: number });
      expect(component.form.invalid).withContext(number).toBeTrue();
    }
    component.form.patchValue({ claimedKurinNameOrNumber: '200' });
    expect(component.form.valid).toBeTrue();
  });

  it('lets a підготовчий курінь apply without a number', () => {
    fillEverythingBut(true);
    component.form.patchValue({ isPreparatoryKurin: true });
    fixture.detectChanges();

    expect(component.form.valid).toBeTrue();
    expect(component.form.value.claimedKurinNameOrNumber).toBeUndefined();
    expect(component.form.value.isPreparatoryKurin).toBeTrue();
    expect(element().querySelector('#kurin')).toBeNull();
  });
});
