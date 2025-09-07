import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GroupChevron } from './group-chevron';

describe('GroupChevron', () => {
  let component: GroupChevron;
  let fixture: ComponentFixture<GroupChevron>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GroupChevron]
    })
    .compileComponents();

    fixture = TestBed.createComponent(GroupChevron);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
