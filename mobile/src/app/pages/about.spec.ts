import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { provideIonicAngular } from '@ionic/angular';
import { AboutPage } from './about';

describe('AboutPage', () => {
  it('counts taps with signals (zoneless)', async () => {
    TestBed.configureTestingModule({ imports: [AboutPage], providers: [provideIonicAngular(), provideHttpClient(), provideRouter([])] });
    const fixture = TestBed.createComponent(AboutPage);
    await fixture.whenStable();

    const button = fixture.nativeElement.querySelector('ion-button') as HTMLElement;
    button.click();
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toContain('Натиснуто: 1 · подвоєно: 2');
  });
});
