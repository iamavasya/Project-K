import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GroupChevronComponent } from './group-chevron';

describe('GroupChevronComponent', () => {
  let component: GroupChevronComponent;
  let fixture: ComponentFixture<GroupChevronComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GroupChevronComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(GroupChevronComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
