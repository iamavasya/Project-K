import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ToolbarHeader } from './toolbar-header';

describe('ToolbarHeader', () => {
  let component: ToolbarHeader;
  let fixture: ComponentFixture<ToolbarHeader>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ToolbarHeader]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ToolbarHeader);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
