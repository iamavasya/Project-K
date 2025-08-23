import { ComponentFixture, TestBed } from '@angular/core/testing';

import { UpsertMemberComponent } from './upsert-member.component';

describe('UpsertMemberComponent', () => {
  let component: UpsertMemberComponent;
  let fixture: ComponentFixture<UpsertMemberComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UpsertMemberComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(UpsertMemberComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
