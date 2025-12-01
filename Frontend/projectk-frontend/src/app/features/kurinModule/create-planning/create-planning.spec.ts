import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CreatePlanning } from './create-planning';

describe('CreatePlanning', () => {
  let component: CreatePlanning;
  let fixture: ComponentFixture<CreatePlanning>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CreatePlanning]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CreatePlanning);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
