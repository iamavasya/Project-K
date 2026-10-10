import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { provideIonicAngular } from '@ionic/angular';
import { AboutPage } from './about';

describe('AboutPage', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [AboutPage],
      providers: [provideIonicAngular(), provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
  });

  it('counts taps with signals (zoneless)', async () => {
    const fixture = TestBed.createComponent(AboutPage);
    await fixture.whenStable();

    const button = Array.from(fixture.nativeElement.querySelectorAll('ion-button') as NodeListOf<HTMLElement>).find(
      (element) => element.textContent?.includes('Натиснути'),
    );
    button!.click();
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toContain('Натиснуто: 1 · подвоєно: 2');
  });

  it('shows the web page’s story and the running API’s version', async () => {
    const fixture = TestBed.createComponent(AboutPage);
    await fixture.whenStable();
    TestBed.inject(HttpTestingController)
      .expectOne((request) => request.url.endsWith('/health'))
      .flush({ version: 'v0.20.0-beta', codeName: 'Honeypot Ant' });
    await fixture.whenStable();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Ростислав Муха');
    expect(text).toContain('Фундамент 1.0');
    expect(text).toContain('v0.20.0-beta «Honeypot Ant»');
  });
});
