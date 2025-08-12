import { ComponentFixture, TestBed } from '@angular/core/testing';

import { KurinPanel } from './kurin-panel';

describe('KurinPanel', () => {
  let component: KurinPanel;
  let fixture: ComponentFixture<KurinPanel>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [KurinPanel]
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
