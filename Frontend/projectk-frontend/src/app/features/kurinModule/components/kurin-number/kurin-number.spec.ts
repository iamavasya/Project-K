import { ComponentFixture, TestBed } from '@angular/core/testing';

import { KurinNumberComponent } from './kurin-number';

describe('KurinNumberComponent', () => {
  let component: KurinNumberComponent;
  let fixture: ComponentFixture<KurinNumberComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [KurinNumberComponent],
    })
    .compileComponents();

    fixture = TestBed.createComponent(KurinNumberComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
