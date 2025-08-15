import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, ActivatedRouteSnapshot, convertToParamMap } from '@angular/router';
import { of } from 'rxjs';

import { GroupPanelComponent } from './group-panel.component';

describe('GroupPanelComponent', () => {
  let component: GroupPanelComponent;
  let fixture: ComponentFixture<GroupPanelComponent>;
  let mockActivatedRoute: Partial<ActivatedRoute>;

  beforeEach(async () => {
    mockActivatedRoute = {
      paramMap: of(convertToParamMap({ kurinKey: 'test-kurin-key' })),
      snapshot: {
        params: { kurinKey: 'test-kurin-key' },
        queryParams: {},
        data: {}
      } as unknown as ActivatedRouteSnapshot
    };

    await TestBed.configureTestingModule({
      imports: [GroupPanelComponent],
      providers: [
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(GroupPanelComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize with empty kurinKey', () => {
    expect(component.kurinKey).toBe('');
  });

  it('should extract kurinKey from route params on init', () => {
    component.ngOnInit();
    
    expect(component.kurinKey).toBe('test-kurin-key');
  });

  it('should handle route param changes', () => {
    // Initial setup
    component.ngOnInit();
    expect(component.kurinKey).toBe('test-kurin-key');

    // Simulate route change
    mockActivatedRoute.paramMap = of(convertToParamMap({ kurinKey: 'new-kurin-key' }));
    component.ngOnInit();
    
    expect(component.kurinKey).toBe('new-kurin-key');
  });

  it('should handle null kurinKey param', () => {
    mockActivatedRoute.paramMap = of(convertToParamMap({ kurinKey: null }));
    
    component.ngOnInit();
    
    expect(component.kurinKey).toBeNull();
  });
});