import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SidebarMenu } from './sidebar-menu';
import { provideHttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { AuthService } from '../../../../authModule/services/authService/auth.service';
import { BehaviorSubject, of } from 'rxjs';
import { AuthState } from '../../../../authModule/models/auth-state.model';
import { SimpleChange } from '@angular/core';

describe('SidebarMenu', () => {
  let component: SidebarMenu;
  let fixture: ComponentFixture<SidebarMenu>;
  let mockRouter: jasmine.SpyObj<Router>;
  let mockAuthService: jasmine.SpyObj<AuthService>;
  let authStateSubject: BehaviorSubject<AuthState | null>;

  beforeEach(async () => {
    authStateSubject = new BehaviorSubject<AuthState | null>(null);
    
    mockRouter = jasmine.createSpyObj('Router', ['navigate']);
    mockAuthService = jasmine.createSpyObj('AuthService', ['getAuthState']);
    mockAuthService.getAuthState.and.returnValue(authStateSubject.asObservable());

    await TestBed.configureTestingModule({
      imports: [SidebarMenu],
      providers: [
        provideHttpClient(),
        { provide: Router, useValue: mockRouter },
        { provide: AuthService, useValue: mockAuthService }
      ],
    })
    .compileComponents();

    fixture = TestBed.createComponent(SidebarMenu);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('ngOnChanges', () => {
    it('should update items$ when state$ changes', (done) => {
      const mockState: AuthState = {
        userKey: 'user-123',
        email: 'test@example.com',
        role: 'Manager',
        kurinKey: 'kurin-456',
        accessToken: 'token-789'
      };

      component.state$ = of(mockState);
      component.ngOnChanges({
        state$: new SimpleChange(null, component.state$, true)
      });

      component.items$.subscribe(items => {
        expect(items.length).toBeGreaterThan(0);
        expect(items.some(item => item.label === 'Курінь')).toBeTrue();
        done();
      });
    });

    it('should update email$ when state$ changes', (done) => {
      const mockState: AuthState = {
        userKey: 'user-123',
        email: 'test@example.com',
        role: 'Manager',
        kurinKey: 'kurin-456',
        accessToken: 'token-789'
      };

      component.state$ = of(mockState);
      component.ngOnChanges({
        state$: new SimpleChange(null, component.state$, true)
      });

      component.email$.subscribe(email => {
        expect(email).toBe('test@example.com');
        done();
      });
    });

    it('should update role$ when state$ changes', (done) => {
      const mockState: AuthState = {
        userKey: 'user-123',
        email: 'test@example.com',
        role: 'Manager',
        kurinKey: 'kurin-456',
        accessToken: 'token-789'
      };

      component.state$ = of(mockState);
      component.ngOnChanges({
        state$: new SimpleChange(null, component.state$, true)
      });

      component.role$.subscribe(role => {
        expect(role).toBe('Manager');
        done();
      });
    });

    it('should set email$ to null when state is null', (done) => {
      component.state$ = of(null);
      component.ngOnChanges({
        state$: new SimpleChange(null, component.state$, true)
      });

      component.email$.subscribe(email => {
        expect(email).toBeNull();
        done();
      });
    });
  });

  describe('buildItems', () => {
    it('should build menu items with kurinKey (Manager view)', (done) => {
      const mockState: AuthState = {
        userKey: 'user-123',
        email: 'test@example.com',
        role: 'Manager',
        kurinKey: 'kurin-456',
        accessToken: 'token-789'
      };

      component.state$ = of(mockState);
      component.ngOnChanges({
        state$: new SimpleChange(null, component.state$, true)
      });

      component.items$.subscribe(items => {
        const kurinItem = items.find(item => item.label === 'Курінь');
        const skillsReviewItem = items.find(item => item.label === 'Модерація вмілостей');
        const panelItem = items.find(item => item.label === 'Panel');
        const usersItem = items.find(item => item.label === 'Users');

        expect(kurinItem).toBeDefined();
        expect(kurinItem?.visible).toBeTrue();
        expect(kurinItem?.disabled).toBeFalse();
        expect(skillsReviewItem?.visible).toBeTrue();
        
        expect(panelItem?.visible).toBeFalse();
        expect(usersItem?.visible).toBeFalse();
        done();
      });
    });

    it('should hide skills moderation item for non-reviewer role', (done) => {
      const mockState: AuthState = {
        userKey: 'user-999',
        email: 'user@example.com',
        role: 'User',
        kurinKey: 'kurin-456',
        accessToken: 'token-789'
      };

      component.state$ = of(mockState);
      component.ngOnChanges({
        state$: new SimpleChange(null, component.state$, true)
      });

      component.items$.subscribe(items => {
        const skillsReviewItem = items.find(item => item.label === 'Модерація вмілостей');
        expect(skillsReviewItem?.visible).toBeFalse();
        done();
      });
    });

    it('should build menu items without kurinKey (Admin view)', (done) => {
      const mockState: AuthState = {
        userKey: 'user-123',
        email: 'admin@example.com',
        role: 'Admin',
        kurinKey: null,
        accessToken: 'token-789'
      };

      component.state$ = of(mockState);
      component.ngOnChanges({
        state$: new SimpleChange(null, component.state$, true)
      });

      component.items$.subscribe(items => {
        const kurinItem = items.find(item => item.label === 'Курінь');
        const panelItem = items.find(item => item.label === 'Panel');
        const usersItem = items.find(item => item.label === 'Users');

        expect(kurinItem?.visible).toBeFalse();
        expect(panelItem).toBeDefined();
        expect(panelItem?.visible).toBeTrue();
        expect(usersItem).toBeDefined();
        expect(usersItem?.visible).toBeTrue();
        done();
      });
    });

    it('should disable kurin-related items when kurinKey is null', (done) => {
      component.state$ = of(null);
      component.ngOnChanges({
        state$: new SimpleChange(null, component.state$, true)
      });

      component.items$.subscribe(items => {
        const kurinRelatedItems = items.filter(item => 
          item.label === 'Курінь' || 
          item.label === 'Гуртки' || 
          item.label === 'Всі учасники' || 
          item.label === 'Налаштування'
        );

        kurinRelatedItems.forEach(item => {
          if (item.label === 'Курінь') {
            expect(item.disabled).toBeTrue();
          }
        });
        done();
      });
    });

    it('should navigate to /kurin when Kurin menu item is clicked', (done) => {
      const mockState: AuthState = {
        userKey: 'user-123',
        email: 'test@example.com',
        role: 'Manager',
        kurinKey: 'kurin-456',
        accessToken: 'token-789'
      };

      component.state$ = of(mockState);
      component.ngOnChanges({
        state$: new SimpleChange(null, component.state$, true)
      });

      component.items$.subscribe(items => {
        const kurinItem = items.find(item => item.label === 'Курінь');
        
        if (kurinItem?.command) {
          kurinItem.command({});
          expect(mockRouter.navigate).toHaveBeenCalledWith(['/kurin']);
        }
        done();
      });
    });

    it('should navigate to /panel when Panel menu item is clicked', (done) => {
      const mockState: AuthState = {
        userKey: 'user-123',
        email: 'admin@example.com',
        role: 'Admin',
        kurinKey: null,
        accessToken: 'token-789'
      };

      component.state$ = of(mockState);
      component.ngOnChanges({
        state$: new SimpleChange(null, component.state$, true)
      });

      component.items$.subscribe(items => {
        const panelItem = items.find(item => item.label === 'Panel');
        
        if (panelItem?.command) {
          panelItem.command({});
          expect(mockRouter.navigate).toHaveBeenCalledWith(['/panel']);
        }
        done();
      });
    });

    it('should navigate to /users when Users menu item is clicked', (done) => {
      const mockState: AuthState = {
        userKey: 'user-123',
        email: 'admin@example.com',
        role: 'Admin',
        kurinKey: null,
        accessToken: 'token-789'
      };

      component.state$ = of(mockState);
      component.ngOnChanges({
        state$: new SimpleChange(null, component.state$, true)
      });

      component.items$.subscribe(items => {
        const usersItem = items.find(item => item.label === 'Users');
        
        if (usersItem?.command) {
          usersItem.command({});
          expect(mockRouter.navigate).toHaveBeenCalledWith(['/users']);
        }
        done();
      });
    });

    it('should navigate to skills review route when moderation item is clicked', (done) => {
      const mockState: AuthState = {
        userKey: 'user-123',
        email: 'mentor@example.com',
        role: 'Mentor',
        kurinKey: 'kurin-456',
        accessToken: 'token-789'
      };

      component.state$ = of(mockState);
      component.ngOnChanges({
        state$: new SimpleChange(null, component.state$, true)
      });

      component.items$.subscribe(items => {
        const skillsReviewItem = items.find(item => item.label === 'Модерація вмілостей');

        if (skillsReviewItem?.command) {
          skillsReviewItem.command({});
          expect(mockRouter.navigate).toHaveBeenCalledWith(['/kurin', 'kurin-456', 'review', 'skills']);
        }
        done();
      });
    });
  });

  describe('close', () => {
    it('should set visible to false', () => {
      component.visible = true;
      component.close();
      expect(component.visible).toBeFalse();
    });

    it('should emit visibleChange event', () => {
      spyOn(component.visibleChange, 'emit');
      component.visible = true;
      component.close();
      expect(component.visibleChange.emit).toHaveBeenCalledWith(false);
    });

    it('should close sidebar when menu item is clicked', (done) => {
      const mockState: AuthState = {
        userKey: 'user-123',
        email: 'test@example.com',
        role: 'Manager',
        kurinKey: 'kurin-456',
        accessToken: 'token-789'
      };

      spyOn(component, 'close');
      component.state$ = of(mockState);
      component.ngOnChanges({
        state$: new SimpleChange(null, component.state$, true)
      });

      component.items$.subscribe(items => {
        const kurinItem = items.find(item => item.label === 'Курінь');
        
        if (kurinItem?.command) {
          kurinItem.command({});
          expect(component.close).toHaveBeenCalled();
        }
        done();
      });
    });
  });

  describe('getSeverityOnRole', () => {
    it('should return "danger" for Admin role', () => {
      expect(component.getSeverityOnRole('Admin')).toBe('danger');
      expect(component.getSeverityOnRole('admin')).toBe('danger');
      expect(component.getSeverityOnRole('ADMIN')).toBe('danger');
    });

    it('should return "warning" for Manager role', () => {
      expect(component.getSeverityOnRole('Manager')).toBe('warning');
      expect(component.getSeverityOnRole('manager')).toBe('warning');
      expect(component.getSeverityOnRole('MANAGER')).toBe('warning');
    });

    it('should return "success" for Mentor role', () => {
      expect(component.getSeverityOnRole('Mentor')).toBe('success');
      expect(component.getSeverityOnRole('mentor')).toBe('success');
      expect(component.getSeverityOnRole('MENTOR')).toBe('success');
    });

    it('should return "info" for unknown roles', () => {
      expect(component.getSeverityOnRole('User')).toBe('info');
      expect(component.getSeverityOnRole('Guest')).toBe('info');
      expect(component.getSeverityOnRole('')).toBe('info');
    });

    it('should return "info" for null role', () => {
      expect(component.getSeverityOnRole(null)).toBe('info');
    });
  });

  describe('Input/Output bindings', () => {
    it('should have visible input property', () => {
      component.visible = true;
      expect(component.visible).toBeTrue();
      
      component.visible = false;
      expect(component.visible).toBeFalse();
    });

    it('should have state$ input property', () => {
      const mockState$ = of(null);
      component.state$ = mockState$;
      expect(component.state$).toBe(mockState$);
    });

    it('should emit visibleChange when changed', () => {
      let emittedValue: boolean | undefined;
      component.visibleChange.subscribe(value => {
        emittedValue = value;
      });

      component.close();
      expect(emittedValue).toBeFalse();
    });
  });

  describe('Integration scenarios', () => {
    it('should update menu items when switching from Manager to Admin', (done) => {
      const managerState: AuthState = {
        userKey: 'user-123',
        email: 'manager@example.com',
        role: 'Manager',
        kurinKey: 'kurin-456',
        accessToken: 'token-789'
      };

      component.state$ = of(managerState);
      component.ngOnChanges({
        state$: new SimpleChange(null, component.state$, true)
      });

      component.items$.subscribe(items => {
        expect(items.find(item => item.label === 'Курінь')?.visible).toBeTrue();
        expect(items.find(item => item.label === 'Panel')?.visible).toBeFalse();

        const adminState: AuthState = {
          ...managerState,
          role: 'Admin',
          kurinKey: null
        };

        component.state$ = of(adminState);
        component.ngOnChanges({
          state$: new SimpleChange(of(managerState), component.state$, false)
        });

        component.items$.subscribe(newItems => {
          expect(newItems.find(item => item.label === 'Курінь')?.visible).toBeFalse();
          expect(newItems.find(item => item.label === 'Panel')?.visible).toBeTrue();
          done();
        });
      });
    });

    it('should show correct menu items based on auth state changes', (done) => {
      component.state$ = authStateSubject.asObservable();
      component.ngOnChanges({
        state$: new SimpleChange(null, component.state$, true)
      });

      const subscription = component.items$.subscribe(items => {
        if (authStateSubject.value === null) {
          expect(items.find(item => item.label === 'Panel')?.visible).toBeTrue();
        } else {
          expect(items.find(item => item.label === 'Курінь')?.visible).toBeTrue();
        }
      });

      authStateSubject.next({
        userKey: 'user-123',
        email: 'test@example.com',
        role: 'Manager',
        kurinKey: 'kurin-456',
        accessToken: 'token-789'
      });

      setTimeout(() => {
        subscription.unsubscribe();
        done();
      }, 0);
    });
  });
});