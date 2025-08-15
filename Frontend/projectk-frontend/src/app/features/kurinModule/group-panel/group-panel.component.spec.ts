import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, ActivatedRouteSnapshot, convertToParamMap, ParamMap } from '@angular/router';
import { Subject } from 'rxjs';

import { GroupPanelComponent } from './group-panel.component';

describe('GroupPanelComponent', () => {
  let component: GroupPanelComponent;
  let fixture: ComponentFixture<GroupPanelComponent>;
  let paramMapSubject: Subject<ParamMap>;

  beforeEach(async () => {
    paramMapSubject = new Subject<ParamMap>();
    
    const mockActivatedRoute = {
      paramMap: paramMapSubject.asObservable(),
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

  afterEach(() => {
    paramMapSubject.complete();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize with empty kurinKey', () => {
    expect(component.kurinKey).toBe('');
  });

  it('should extract kurinKey from route params on init', () => {
    component.ngOnInit();
    paramMapSubject.next(convertToParamMap({ kurinKey: 'test-kurin-key' }));
    
    expect(component.kurinKey).toBe('test-kurin-key');
  });

  it('should handle route param changes', () => {
    component.ngOnInit();
    
    // Initial value
    paramMapSubject.next(convertToParamMap({ kurinKey: 'test-kurin-key' }));
    expect(component.kurinKey).toBe('test-kurin-key');
    
    // Changed value
    paramMapSubject.next(convertToParamMap({ kurinKey: 'new-kurin-key' }));
    expect(component.kurinKey).toBe('new-kurin-key');
  });

  it('should handle missing kurinKey param', () => {
    component.ngOnInit();
    paramMapSubject.next(convertToParamMap({}));

    expect(component.kurinKey).toBeNull();
  });

  it('should handle null kurinKey param', () => {
    component.ngOnInit();
    paramMapSubject.next(convertToParamMap({ kurinKey: null }));
    
    expect(component.kurinKey).toBeNull();
  });
});