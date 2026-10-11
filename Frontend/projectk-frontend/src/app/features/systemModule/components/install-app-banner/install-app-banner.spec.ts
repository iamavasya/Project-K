import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { InstallAppBannerComponent } from './install-app-banner';
import { InstallAppService } from '../../services/install-app-service/install-app.service';

/** The service's decisions are install-app.function's; here only what the banner does with them. */
function fakeService(canPrompt: boolean, preferWeb = false) {
  const shouldOffer = signal(!preferWeb);
  return {
    shouldOffer,
    offersMobileVersion: signal(preferWeb),
    forgetPreferWeb: jasmine.createSpy('forgetPreferWeb'),
    canPrompt: signal(canPrompt),
    prompt: jasmine.createSpy('prompt').and.resolveTo(true),
    dismiss: jasmine.createSpy('dismiss').and.callFake(() => shouldOffer.set(false))
  };
}

describe('InstallAppBannerComponent', () => {
  let fixture: ComponentFixture<InstallAppBannerComponent>;
  let service: ReturnType<typeof fakeService>;
  let goTo: jasmine.Spy;

  async function create(canPrompt: boolean, preferWeb = false): Promise<void> {
    service = fakeService(canPrompt, preferWeb);
    await TestBed.configureTestingModule({
      imports: [InstallAppBannerComponent],
      providers: [provideRouter([]), { provide: InstallAppService, useValue: service }]
    }).compileComponents();
    fixture = TestBed.createComponent(InstallAppBannerComponent);
    // A real navigation would take the Karma page with it.
    goTo = spyOn(fixture.componentInstance as unknown as { goTo: (url: string) => void }, 'goTo');
    fixture.detectChanges();
  }

  function button(label: string): HTMLButtonElement {
    const buttons = Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[];
    const found = buttons.find(b => b.textContent?.includes(label));
    if (!found) {
      throw new Error(`no «${label}» button`);
    }
    return found;
  }

  it('should install in one tap where Chrome has offered its dialog', async () => {
    await create(true);
    expect(fixture.nativeElement.textContent).toContain('Лілейка на телефоні');
    button('Встановити').click();
    await fixture.whenStable();
    expect(service.prompt).toHaveBeenCalled();
    expect(goTo).not.toHaveBeenCalled();
  });

  it('should open the step-by-step page elsewhere', async () => {
    await create(false);
    button('Встановити').click();
    await fixture.whenStable();
    expect(service.prompt).not.toHaveBeenCalled();
    expect(goTo).toHaveBeenCalledWith('/m/install');
  });

  it('should continue on the same screen in the phone app', async () => {
    await create(false);
    spyOnProperty(TestBed.inject(Router), 'url').and.returnValue('/calendar/k1');
    button('Відкрити мобільну версію').click();
    expect(goTo).toHaveBeenCalledWith('/m/tabs/calendar');
  });

  it('should give a phone that chose «Повна версія» one quiet way back', async () => {
    await create(false, true);
    expect(fixture.nativeElement.querySelector('[data-testid=install-app-banner]')).toBeNull();
    spyOnProperty(TestBed.inject(Router), 'url').and.returnValue('/');
    button('Мобільна версія').click();
    expect(service.forgetPreferWeb).toHaveBeenCalled();
    expect(goTo).toHaveBeenCalledWith('/m/tabs/home');
  });

  it('should go away on «Не зараз»', async () => {
    await create(false);
    button('Не зараз').click();
    fixture.detectChanges();
    expect(service.dismiss).toHaveBeenCalled();
    expect(fixture.nativeElement.querySelector('[data-testid=install-app-banner]')).toBeNull();
  });
});
