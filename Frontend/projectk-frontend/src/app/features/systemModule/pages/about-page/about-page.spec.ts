import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AboutPageComponent } from './about-page';
import { environment } from '../../../../../environments/environment';

describe('AboutPageComponent', () => {
  let fixture: ComponentFixture<AboutPageComponent>;
  let component: AboutPageComponent;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AboutPageComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    }).compileComponents();

    fixture = TestBed.createComponent(AboutPageComponent);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('asks the API beside /api for its version and shows both', () => {
    fixture.detectChanges();
    const base = environment.apiUrl.endsWith('/') ? environment.apiUrl.slice(0, -1) : environment.apiUrl;
    const expected = (base.endsWith('/api') ? base.slice(0, -4) : base) + '/health';
    http.expectOne(expected).flush({ version: '1.0.0', codeName: 'Fire Ant' });
    fixture.detectChanges();

    expect(component.frontendVersion).toBe(environment.version);
    expect(component.apiVersion()).toBe('1.0.0');
    expect(component.apiCodeName()).toBe('Fire Ant');
  });

  it('still renders when the API is out of reach', () => {
    fixture.detectChanges();
    http.expectOne(() => true).error(new ProgressEvent('error'));
    fixture.detectChanges();

    expect(component.apiVersion()).toBeNull();
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Ростислав Муха');
    expect(text).toContain('Ідея');
  });

  it('tells the story in order, from the idea to 1.0', () => {
    http.expectOne(() => true).flush({});

    expect(component.milestones[0].title).toBe('Ідея');
    expect(component.milestones[component.milestones.length - 1].title).toContain('1.0');
  });

  // One spelling for every release tag: the suffix git knows, then the code name when there was one.
  it('spells every release as its git tag, with the code name when it had one', () => {
    http.expectOne(() => true).flush({});

    expect(component.releaseLabel({ tag: 'v0.13.0-beta', codeName: 'Queen Ant' })).toBe('v0.13.0-beta «Queen Ant»');
    expect(component.releaseLabel({ tag: 'v0.1.0-alpha.1' })).toBe('v0.1.0-alpha.1');
    const tags = component.milestones.flatMap(m => m.releases ?? []).map(r => r.tag);
    expect(tags.every(tag => /^v\d+\.\d+\.\d+(-(alpha|beta)(\.\d+)?)?$/.test(tag))).toBeTrue();
  });
});
