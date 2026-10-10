import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideIonicAngular } from '@ionic/angular';
import { HomePage } from './home';

describe('HomePage', () => {
  it('counts taps with signals (zoneless)', async () => {
    TestBed.configureTestingModule({ imports: [HomePage], providers: [provideIonicAngular(), provideHttpClient()] });
    const fixture = TestBed.createComponent(HomePage);
    await fixture.whenStable();

    const button = fixture.nativeElement.querySelector('ion-button') as HTMLElement;
    button.click();
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toContain('Натиснуто: 1 · подвоєно: 2');
  });
});
