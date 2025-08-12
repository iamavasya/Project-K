import { ComponentFixture, TestBed } from '@angular/core/testing';

import { KurinPanel } from './kurin-panel';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

describe('KurinPanel', () => {
  let component: KurinPanel;
  let fixture: ComponentFixture<KurinPanel>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [KurinPanel],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(KurinPanel);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
