import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { KurinPanelComponent } from './kurin-panel.component';
import { KurinService } from '../common/services/kurin-service/kurin.service';
import { KurinDto } from '../common/models/kurinDto';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';

describe('KurinPanel', () => {
  let component: KurinPanelComponent;
  let fixture: ComponentFixture<KurinPanelComponent>;
  let kurinService: jasmine.SpyObj<KurinService>;
  let mockKurins: KurinDto[];

  beforeEach(async () => {
    // Mock data
    mockKurins = [
      { kurinKey: '1', number: 101 },
      { kurinKey: '2', number: 102 }
    ];

    // Create spy object
    const kurinServiceSpy = jasmine.createSpyObj('KurinService', 
      ['getKurins', 'createKurin', 'updateKurin', 'deleteKurin']);
    await TestBed.configureTestingModule({
      imports: [KurinPanelComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideNoopAnimations(),
        { provide: KurinService, useValue: kurinServiceSpy }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(KurinPanelComponent);
    component = fixture.componentInstance;
    kurinService = TestBed.inject(KurinService) as jasmine.SpyObj<KurinService>;
    
    // Setup default spy returns
    kurinService.getKurins.and.returnValue(of(mockKurins));
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize with default values', () => {
    expect(component.data).toEqual([]);
    expect(component.selectedItem).toBeNull();
    expect(component.managePanelVisible).toBeFalse();
    expect(component.managePanelParameter).toBe('undef');
  });

  it('should fetch kurins on init', () => {
    component.ngOnInit();
    
    expect(kurinService.getKurins).toHaveBeenCalled();
    expect(component.data).toEqual(mockKurins);
  });

  it('should handle empty kurins data', () => {
    kurinService.getKurins.and.returnValue(of(null as unknown as KurinDto[]));
    
    component.refreshData();
    
    expect(component.data).toEqual([]);
  });

  it('should show message when no kurins are available', () => {
    kurinService.getKurins.and.returnValue(of([]));
    
    component.refreshData();
    fixture.detectChanges();
    
    expect(component.data).toEqual([]);
    expect(component.data.length).toBe(0);
    expect(fixture.nativeElement.querySelector('p-message')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('p-message').textContent).toContain('Наразі немає доступних куренів. Створіть один!');
  });

  describe('prepareItemActions', () => {
    it('should prepare actions for an item', () => {
      const testKurin = mockKurins[0];
      
      component.prepareItemActions(testKurin);
      
      expect(component.actions).toHaveSize(2);
      expect(component.actions[0].label).toBe('Update');
      expect(component.actions[1].label).toBe('Delete');
    });
  });

  describe('onActionClick', () => {
    it('should set selected item and show manage panel', () => {
      const testKurin = mockKurins[0];
      
      component.onActionClick(testKurin, 'update');
      
      expect(component.selectedItem).toBe(testKurin);
      expect(component.managePanelVisible).toBeTrue();
      expect(component.managePanelParameter).toBe('update');
    });

    it('should handle create action with null item', () => {
      component.onActionClick(null, 'create');
      
      expect(component.selectedItem).toBeNull();
      expect(component.managePanelVisible).toBeTrue();
      expect(component.managePanelParameter).toBe('create');
    });
  });

  describe('actionHandler', () => {
    beforeEach(() => {
      kurinService.createKurin.and.returnValue(of({ kurinKey: '3', number: 103 }));
      kurinService.updateKurin.and.returnValue(of({ kurinKey: '1', number: 201 }));
      kurinService.deleteKurin.and.returnValue(of(undefined));
    });

    it('should handle create action', () => {
      const newKurin = { kurinKey: '3', number: 103 };
      
      component.actionHandler({ action: 'create', kurin: newKurin });
      
      expect(kurinService.createKurin).toHaveBeenCalledWith(newKurin);
      expect(kurinService.getKurins).toHaveBeenCalled();
    });

    it('should handle update action', () => {
      const updatedKurin = { kurinKey: '1', number: 201 };
      
      component.actionHandler({ action: 'update', kurin: updatedKurin });
      
      expect(kurinService.updateKurin).toHaveBeenCalledWith(updatedKurin);
      expect(kurinService.getKurins).toHaveBeenCalled();
    });

    it('should handle delete action', () => {
      const kurinToDelete = { kurinKey: '1', number: 101 };
      
      component.actionHandler({ action: 'delete', kurin: kurinToDelete });
      
      expect(kurinService.deleteKurin).toHaveBeenCalledWith('1');
      expect(kurinService.getKurins).toHaveBeenCalled();
    });
  });

  // describe('onOpenClick', () => {
  //   it('should show alert when open is clicked', () => {
  //     spyOn(window, 'alert');
      
  //     component.onOpenClick();
      
  //     expect(window.alert).toHaveBeenCalledWith('Open functionality is not implemented yet.');
  //   });
  // });
  

  describe('refreshData', () => {
    it('should refresh data from service', () => {
      component.refreshData();
      
      expect(kurinService.getKurins).toHaveBeenCalled();
      expect(component.data).toEqual(mockKurins);
    });
  });
});