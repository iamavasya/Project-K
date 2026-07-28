import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { AdminPanelComponent } from './admin-panel.component';
import { KurinService } from '../common/services/kurin-service/kurin.service';
import { AuthService } from '../../authModule/services/authService/auth.service';
import { KurinDto } from '../common/models/kurinDto';
import { AuthState } from '../../authModule/models/auth-state.model';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { Router, provideRouter } from '@angular/router';
import { MessageService } from 'primeng/api';

describe('AdminPanelComponent', () => {
  let component: AdminPanelComponent;
  let fixture: ComponentFixture<AdminPanelComponent>;
  let kurinService: jasmine.SpyObj<KurinService>;
  let authService: jasmine.SpyObj<AuthService>;

  const mockKurins: KurinDto[] = [
    { kurinKey: '1', number: 101 },
    { kurinKey: '2', number: 102 }
  ];

  const scopedState: AuthState = {
    userKey: 'user-1',
    memberKey: null,
    email: 'admin@projectk.com',
    role: 'Admin',
    kurinKey: '1',
    accessToken: 'scoped-token'
  };

  beforeEach(async () => {
    const kurinServiceSpy = jasmine.createSpyObj('KurinService',
      ['getKurins', 'createKurin', 'updateKurin', 'deleteKurin']);
    const authServiceSpy = jasmine.createSpyObj('AuthService',
      ['registerFirstManager', 'setKurinKey', 'setKurinScope']);

    await TestBed.configureTestingModule({
      imports: [AdminPanelComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideNoopAnimations(),
        provideRouter([]),
        { provide: KurinService, useValue: kurinServiceSpy },
        { provide: AuthService, useValue: authServiceSpy },
        MessageService
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AdminPanelComponent);
    component = fixture.componentInstance;
    kurinService = TestBed.inject(KurinService) as jasmine.SpyObj<KurinService>;
    authService = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;

    kurinService.getKurins.and.returnValue(of(mockKurins));
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('initial state before ngOnInit', () => {
    expect(component.data).toEqual([]);
    expect(component.selectedItem).toBeNull();
    expect(component.managePanelVisible).toBeFalse();
    expect(component.managePanelParameter).toBe('undef');
  });

  it('should fetch kurins on init', () => {
    component.ngOnInit();
    expect(kurinService.getKurins).toHaveBeenCalledTimes(1);
    expect(component.data).toEqual(mockKurins);
  });

  it('refreshData sets empty array on null response', () => {
    kurinService.getKurins.and.returnValue(of(null as unknown as KurinDto[]));
    component.refreshData();
    expect(component.data).toEqual([]);
  });

  it('should show no data message when list empty (template)', () => {
    kurinService.getKurins.and.returnValue(of([]));
    component.refreshData();
    fixture.detectChanges();
    expect(component.data.length).toBe(0);
    const empty = fixture.nativeElement.querySelector('app-empty-state');
    expect(empty).toBeTruthy();
    expect(empty.textContent).toContain('Куренів ще немає');
  });

  describe('prepareItemActions', () => {
    it('creates Update/Delete menu items', () => {
      component.prepareItemActions(mockKurins[0]);
      expect(component.actions.length).toBe(2);
      expect(component.actions[0].label).toBe('Редагувати');
      expect(component.actions[1].label).toBe('Видалити');
    });
  });

  describe('onActionClick', () => {
    it('opens panel for update', () => {
      component.onActionClick(mockKurins[0], 'update');
      expect(component.selectedItem).toBe(mockKurins[0]);
      expect(component.managePanelVisible).toBeTrue();
      expect(component.managePanelParameter).toBe('update');
    });

    it('opens panel for create (selectedItem null)', () => {
      component.onActionClick(null, 'create');
      expect(component.selectedItem).toBeNull();
      expect(component.managePanelVisible).toBeTrue();
      expect(component.managePanelParameter).toBe('create');
    });
  });

  describe('onManageAction', () => {
    beforeEach(() => {
      component.ngOnInit(); // baseline fetch
      authService.registerFirstManager.and.returnValue(of(void 0));
      kurinService.updateKurin.and.returnValue(of({ kurinKey: '1', number: 201 }));
      kurinService.deleteKurin.and.returnValue(of(void 0));
    });

    it('handles create', () => {
      const newEntity: KurinDto = { kurinKey: '3', number: 103, managerEmail: 'test@example.com' };
      component.onManageAction({ action: 'create', entity: newEntity, entityType: 'kurin' });
      expect(authService.registerFirstManager).toHaveBeenCalledWith(newEntity);
      expect(kurinService.getKurins).toHaveBeenCalledTimes(2); // initial + refresh
    });

    it('handles update', () => {
      const updated: KurinDto = { kurinKey: '1', number: 201 };
      component.onManageAction({ action: 'update', entity: updated, entityType: 'kurin' });
      expect(kurinService.updateKurin).toHaveBeenCalledWith(updated);
      expect(kurinService.getKurins).toHaveBeenCalledTimes(2);
    });

    it('handles delete', () => {
      const toDelete: KurinDto = { kurinKey: '1', number: 101 };
      component.onManageAction({ action: 'delete', entity: toDelete, entityType: 'kurin' });
      expect(kurinService.deleteKurin).toHaveBeenCalledWith('1');
      expect(kurinService.getKurins).toHaveBeenCalledTimes(2);
    });
  });

  describe('onOpenClick', () => {
    it('scopes the admin into the kurin before navigating', () => {
      const router = TestBed.inject(Router);
      const navigate = spyOn(router, 'navigate');
      authService.setKurinScope.and.returnValue(of(scopedState));

      component.onOpenClick('1');

      expect(authService.setKurinScope).toHaveBeenCalledWith('1');
      expect(navigate).toHaveBeenCalledWith(['/kurin']);
    });

    it('stays on the panel when scoping fails', () => {
      const router = TestBed.inject(Router);
      const navigate = spyOn(router, 'navigate');
      authService.setKurinScope.and.returnValue(throwError(() => new Error('nope')) as never);

      component.onOpenClick('1');

      expect(navigate).not.toHaveBeenCalled();
    });
  });

  describe('managePanelConfig', () => {
    it('has correct field rules (kurinKey hidden on create)', () => {
      const kurinKeyField = component.managePanelConfig.fields.find(f => f.name === 'kurinKey')!;
      expect(kurinKeyField.hiddenOn).toContain('create');
      expect(kurinKeyField.disabledOn).toContain('update');
    });
  });
});
