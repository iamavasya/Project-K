import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { BehaviorSubject, of } from 'rxjs';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

import { KurinPanelComponent } from './kurin-panel.component';
import { GroupService } from '../common/services/group-service/group.service';
import { KurinService } from '../common/services/kurin-service/kurin.service';
import { AuthService } from '../../authModule/services/authService/auth.service';
import { GroupDto } from '../common/models/groupDto';
import { AuthState } from '../../authModule/models/auth-state.model';

describe('KurinPanelComponent', () => {
  let fixture: ComponentFixture<KurinPanelComponent>;
  let component: KurinPanelComponent;

  let groupService: jasmine.SpyObj<GroupService>;
  let kurinService: jasmine.SpyObj<KurinService>;
  let authService: jasmine.SpyObj<AuthService>;
  let authState$: BehaviorSubject<AuthState | null>;

  const mockAuthState: AuthState = {
    userKey: 'u1',
    email: 'test@example.com',
    role: 'admin',
    kurinKey: 'k1',
    accessToken: 'token'
  };

  const mockGroups: GroupDto[] = [
    { groupKey: 'g1', name: 'Alpha', kurinKey: 'k1', kurinNumber: 10 },
    { groupKey: 'g2', name: 'Beta',  kurinKey: 'k1', kurinNumber: 10 }
  ];

  beforeEach(async () => {
    authState$ = new BehaviorSubject<AuthState | null>(mockAuthState);

    groupService = jasmine.createSpyObj<GroupService>('GroupService', [
      'getAllByKurinKey', 'create', 'update', 'delete'
    ]);
    kurinService = jasmine.createSpyObj<KurinService>('KurinService', [
      'getByKey'
    ]);
    authService = jasmine.createSpyObj<AuthService>('AuthService', [
      'getAuthState'
    ]);

    groupService.getAllByKurinKey.and.returnValue(of(mockGroups));
    kurinService.getByKey.and.returnValue(of({ kurinKey: 'k1', number: 10 }));
    authService.getAuthState.and.returnValue(authState$.asObservable());

    await TestBed.configureTestingModule({
      imports: [KurinPanelComponent],
      providers: [
        { provide: ActivatedRoute, useValue: {} },
        { provide: GroupService, useValue: groupService },
        { provide: KurinService, useValue: kurinService },
        { provide: AuthService, useValue: authService },
        provideHttpClient(),
        provideHttpClientTesting(),
        provideNoopAnimations()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(KurinPanelComponent);
    component = fixture.componentInstance;
  });

  afterEach(() => {
    authState$.complete();
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

  it('on init gets kurinKey from authState and calls refreshData', () => {
    fixture.detectChanges(); // triggers ngOnInit
    expect(authService.getAuthState).toHaveBeenCalled();
    expect(component.kurinKey).toBe('k1');
    expect(groupService.getAllByKurinKey).toHaveBeenCalledWith('k1');
    expect(kurinService.getByKey).toHaveBeenCalledWith('k1');
    expect(component.groups).toEqual(mockGroups);
  });

  it('authState emission updates kurinKey', () => {
    fixture.detectChanges();
    groupService.getAllByKurinKey.calls.reset();
    const newAuthState: AuthState = {
      ...mockAuthState,
      kurinKey: 'k2'
    };
    authState$.next(newAuthState);
    expect(component.kurinKey).toBe('k2');
  });

  it('manual refresh uses current kurinKey from component', () => {
    fixture.detectChanges();
    component.kurinKey = 'k2';
    groupService.getAllByKurinKey.calls.reset();
    kurinService.getByKey.calls.reset();
    component.refreshData();
    expect(groupService.getAllByKurinKey).toHaveBeenCalledWith('k2');
    expect(kurinService.getByKey).toHaveBeenCalledWith('k2');
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