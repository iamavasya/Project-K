import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, ActivatedRouteSnapshot, convertToParamMap } from '@angular/router';
import { of } from 'rxjs';

import { GroupPanelComponent } from './group-panel.component';

describe('GroupPanelComponent', () => {
  let component: GroupPanelComponent;
  let fixture: ComponentFixture<GroupPanelComponent>;

  const createMockActivatedRoute = (kurinKey: string | null = 'test-kurin-key') => ({
    paramMap: of(convertToParamMap(kurinKey ? { kurinKey } : {})),
    snapshot: {
      params: kurinKey ? { kurinKey } : {},
      queryParams: {},
      data: {}
    } as unknown as ActivatedRouteSnapshot
  });

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GroupPanelComponent],
      providers: [
        { provide: ActivatedRoute, useValue: createMockActivatedRoute() }
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

  it('should handle route param changes', async () => {
    // Create new component with different route params
    await TestBed.overrideProvider(ActivatedRoute, {
      useValue: createMockActivatedRoute('new-kurin-key')
    }).compileComponents();

    const newFixture = TestBed.createComponent(GroupPanelComponent);
    const newComponent = newFixture.componentInstance;
    
    newComponent.ngOnInit();
    
    expect(newComponent.kurinKey).toBe('new-kurin-key');
  });

  it('should handle missing kurinKey param', async () => {
    // Create new component with no route params
    await TestBed.overrideProvider(ActivatedRoute, {
      useValue: createMockActivatedRoute(null)
    }).compileComponents();

    const newFixture = TestBed.createComponent(GroupPanelComponent);
    const newComponent = newFixture.componentInstance;
    
    newComponent.ngOnInit();
    
    expect(newComponent.kurinKey).toBeNull();
  });
});