import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ManagePanel } from './manage-panel';

describe('ManagePanel', () => {
  let component: ManagePanel;
  let fixture: ComponentFixture<ManagePanel>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ManagePanel]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ManagePanel);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
