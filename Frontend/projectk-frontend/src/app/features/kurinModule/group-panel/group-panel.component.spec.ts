import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, ActivatedRouteSnapshot, ParamMap, convertToParamMap } from '@angular/router';
import { Subject, of } from 'rxjs';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

import { GroupPanelComponent } from './group-panel.component';
import { GroupService } from '../common/services/group-service/group.service';
import { KurinService } from '../common/services/kurin-service/kurin.service';
import { GroupDto } from '../common/models/groupDto';

describe('GroupPanelComponent', () => {
  let fixture: ComponentFixture<GroupPanelComponent>;
  let component: GroupPanelComponent;

  let groupService: jasmine.SpyObj<GroupService>;
  let kurinService: jasmine.SpyObj<KurinService>;
  let paramMap$: Subject<ParamMap>;

  const mockGroups: GroupDto[] = [
    { groupKey: 'g1', name: 'Alpha', kurinKey: 'k1', kurinNumber: 10 },
    { groupKey: 'g2', name: 'Beta',  kurinKey: 'k1', kurinNumber: 10 }
  ];

  beforeEach(async () => {
    paramMap$ = new Subject<ParamMap>();

    const activatedRouteStub = {
      paramMap: paramMap$.asObservable(),
      snapshot: { params: { kurinKey: 'k1' } } as unknown as ActivatedRouteSnapshot
    };

    groupService = jasmine.createSpyObj<GroupService>('GroupService', [
      'getAllByKurinKey', 'create', 'update', 'delete'
    ]);
    kurinService = jasmine.createSpyObj<KurinService>('KurinService', [
      'getByKey'
    ]);

    groupService.getAllByKurinKey.and.returnValue(of(mockGroups));
    kurinService.getByKey.and.returnValue(of({ kurinKey: 'k1', number: 10 }));

    await TestBed.configureTestingModule({
      imports: [GroupPanelComponent],
      providers: [
        { provide: ActivatedRoute, useValue: activatedRouteStub },
        { provide: GroupService, useValue: groupService },
        { provide: KurinService, useValue: kurinService },
        provideHttpClient(),
        provideHttpClientTesting(),
        provideNoopAnimations()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(GroupPanelComponent);
    component = fixture.componentInstance;
  });

  afterEach(() => {
    paramMap$.complete();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('initial state before ngOnInit', () => {
    expect(component.kurinKey).toBe('');
    expect(component.groups).toEqual([]);
    expect(component.groupPanelVisible).toBeFalse();
    expect(component.groupPanelParameter).toBe('undef');
  });

  it('on init calls refreshData once with empty kurinKey', () => {
    fixture.detectChanges(); // triggers ngOnInit -> refreshData()
    expect(groupService.getAllByKurinKey).toHaveBeenCalledTimes(1);
    expect(groupService.getAllByKurinKey).toHaveBeenCalledWith('');
    expect(component.groups).toEqual(mockGroups);
  });

  it('param emission updates kurinKey but does NOT auto refresh again', () => {
    fixture.detectChanges();
    groupService.getAllByKurinKey.calls.reset();
    paramMap$.next(convertToParamMap({ kurinKey: 'k1' }));
    expect(component.kurinKey).toBe('k1');
    expect(groupService.getAllByKurinKey).not.toHaveBeenCalled();
  });

  it('manual refresh after param emission uses updated kurinKey', () => {
    fixture.detectChanges();
    paramMap$.next(convertToParamMap({ kurinKey: 'k1' }));
    groupService.getAllByKurinKey.calls.reset();
    component.refreshData();
    expect(groupService.getAllByKurinKey).toHaveBeenCalledWith('k1');
  });

  describe('prepareItemActions', () => {
    it('sets Update/Delete actions', () => {
      component.prepareItemActions(mockGroups[0]);
      expect(component.actions.length).toBe(2);
      expect(component.actions[0].label).toBe('Update');
      expect(component.actions[1].label).toBe('Delete');
    });
  });

  describe('onGroupActionClick', () => {
    it('create: resets selectedGroup', () => {
      component.onGroupActionClick(null, 'create');
      expect(component.selectedGroup).toBeNull();
      expect(component.groupPanelParameter).toBe('create');
      expect(component.groupPanelVisible).toBeTrue();
    });

    it('update: sets selectedGroup', () => {
      component.onGroupActionClick(mockGroups[0], 'update');
      expect(component.selectedGroup).toBe(mockGroups[0]);
      expect(component.groupPanelParameter).toBe('update');
      expect(component.groupPanelVisible).toBeTrue();
    });
  });

  describe('onGroupManage', () => {
    beforeEach(() => {
      component.kurinKey = 'k1';
      groupService.create.and.returnValue(of(mockGroups[0]));
      groupService.update.and.returnValue(of(mockGroups[0]));
      groupService.delete.and.returnValue(of(void 0));
    });

    it('create calls service.create with full entity', () => {
      const entity = { groupKey: '', name: 'Gamma', kurinKey: 'k1', kurinNumber: 10 };
      component.onGroupManage({ action: 'create', entity });
      expect(groupService.create).toHaveBeenCalledWith(entity);
      expect(groupService.getAllByKurinKey).toHaveBeenCalled();
    });

    it('update calls service.update with key + entity', () => {
      const entity = { groupKey: 'g1', name: 'Alpha*', kurinKey: 'k1', kurinNumber: 10 };
      component.onGroupManage({ action: 'update', entity });
      expect(groupService.update).toHaveBeenCalledWith('g1', entity);
      expect(groupService.getAllByKurinKey).toHaveBeenCalled();
    });

    it('delete calls service.delete with key', () => {
      const entity = { groupKey: 'g1', name: 'Alpha', kurinKey: 'k1', kurinNumber: 10 };
      component.onGroupManage({ action: 'delete', entity });
      expect(groupService.delete).toHaveBeenCalledWith('g1');
      expect(groupService.getAllByKurinKey).toHaveBeenCalled();
    });
  });

  describe('groupPanelConfig', () => {
    it('groupKey field rules', () => {
      const f = component.groupPanelConfig.fields.find(x => x.name === 'groupKey')!;
      expect(f.hiddenOn).toContain('create');
      expect(f.disabledOn).toContain('update');
    });
    it('kurinKey hidden on update', () => {
      const f = component.groupPanelConfig.fields.find(x => x.name === 'kurinKey')!;
      expect(f.hiddenOn).toContain('update');
    });
    it('name visible on create', () => {
      const f = component.groupPanelConfig.fields.find(x => x.name === 'name')!;
      expect((f.hiddenOn || []).includes('create')).toBeFalse();
    });
  });
});