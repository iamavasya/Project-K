import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ManagePanel, ManagePanelConfig, ManagePanelField } from './manage-panel';
import { SimpleChange } from '@angular/core';
import { Validators } from '@angular/forms';

describe('ManagePanel', () => {
  let component: ManagePanel;
  let fixture: ComponentFixture<ManagePanel>;

  const mockConfig: ManagePanelConfig = {
    entityType: 'TestEntity',
    title: 'Test Entity Management',
    fields: [
      {
        name: 'id',
        label: 'ID',
        type: 'number',
        required: false,
        disabledOn: ['update', 'delete']
      },
      {
        name: 'name',
        label: 'Name',
        type: 'text',
        placeholder: 'Enter name',
        required: true
      },
      {
        name: 'description',
        label: 'Description',
        type: 'textarea',
        required: false,
        hiddenOn: ['delete']
      },
      {
        name: 'value',
        label: 'Value',
        type: 'number',
        required: true
      }
    ],
    createFactory: () => ({ id: null, name: '', description: '', value: 0 }),
    mapOut: (raw) => ({ ...raw, mapped: true })
  };

  const mockEntity = {
    id: 1,
    name: 'Test Item',
    description: 'Test Description',
    value: 100
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ManagePanel]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ManagePanel);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Component initialization', () => {
    it('should initialize with default values', () => {
      expect(component.visible).toBeFalse();
      expect(component.parameter).toBe('undef');
      expect(component.entity).toBeNull();
      expect(component.ready).toBeFalse();
    });

    it('should build form when config is provided', () => {
      component.config = mockConfig;
      component.ngOnChanges({
        config: new SimpleChange(null, mockConfig, true)
      });

      expect(component.form).toBeDefined();
      expect(component.ready).toBeTrue();
      expect(Object.keys(component.form.controls).length).toBe(4);
    });
  });

  describe('ngOnChanges', () => {
    beforeEach(() => {
      component.config = mockConfig;
      component.ngOnChanges({
        config: new SimpleChange(null, mockConfig, true)
      });
    });

    it('should rebuild form when config changes', () => {
      const newConfig = { ...mockConfig, entityType: 'NewEntity' };
      
      component.config = newConfig;
      component.ngOnChanges({
        config: new SimpleChange(mockConfig, newConfig, false)
      });

      expect(component.form).toBeDefined();
      expect(component.ready).toBeTrue();
    });

    it('should patch entity when entity changes', () => {
      component.entity = mockEntity;
      component.ngOnChanges({
        entity: new SimpleChange(null, mockEntity, false)
      });

      expect(component.form.get('name')?.value).toBe('Test Item');
      expect(component.form.get('description')?.value).toBe('Test Description');
      expect(component.form.get('value')?.value).toBe(100);
    });

    it('should apply state when parameter changes', () => {
      component.parameter = 'create';
      component.ngOnChanges({
        parameter: new SimpleChange('undef', 'create', false)
      });

      expect(component.form.get('id')?.disabled).toBeFalse();
    });

    it('should reset form when creating new entity', () => {
      component.parameter = 'create';
      component.visible = true;
      
      component.ngOnChanges({
        visible: new SimpleChange(false, true, false),
        parameter: new SimpleChange('undef', 'create', false)
      });

      expect(component.form.get('name')?.pristine).toBeTrue();
      expect(component.form.get('name')?.untouched).toBeTrue();
    });

    it('should patch entity when visible for update', () => {
      component.parameter = 'update';
      component.entity = mockEntity;
      component.visible = true;

      component.ngOnChanges({
        visible: new SimpleChange(false, true, false)
      });

      expect(component.form.get('name')?.value).toBe('Test Item');
    });
  });

  describe('buildForm', () => {
    it('should create form controls for all fields', () => {
      component.config = mockConfig;
      component.ngOnChanges({
        config: new SimpleChange(null, mockConfig, true)
      });

      expect(component.form.get('id')).toBeDefined();
      expect(component.form.get('name')).toBeDefined();
      expect(component.form.get('description')).toBeDefined();
      expect(component.form.get('value')).toBeDefined();
    });

    it('should add required validators to required fields', () => {
      component.config = mockConfig;
      component.ngOnChanges({
        config: new SimpleChange(null, mockConfig, true)
      });

      const nameControl = component.form.get('name');
      const descriptionControl = component.form.get('description');

      expect(nameControl?.hasValidator(Validators.required)).toBeTrue();
      expect(descriptionControl?.hasValidator(Validators.required)).toBeFalse();
    });

    it('should use createFactory when no entity provided', () => {
      component.config = mockConfig;
      component.entity = null;
      component.ngOnChanges({
        config: new SimpleChange(null, mockConfig, true)
      });

      expect(component.form.get('name')?.value).toBe('');
      expect(component.form.get('value')?.value).toBe(0);
    });

    it('should use entity values when provided', () => {
      component.config = mockConfig;
      component.entity = mockEntity;
      component.ngOnChanges({
        config: new SimpleChange(null, mockConfig, true)
      });

      expect(component.form.get('name')?.value).toBe('Test Item');
      expect(component.form.get('value')?.value).toBe(100);
    });
  });

  describe('applyStatePerAction', () => {
    beforeEach(() => {
      component.config = mockConfig;
      component.ngOnChanges({
        config: new SimpleChange(null, mockConfig, true)
      });
    });

    it('should disable all fields for delete action', () => {
      component.parameter = 'delete';
      component.ngOnChanges({
        parameter: new SimpleChange('undef', 'delete', false)
      });

      expect(component.form.get('id')?.disabled).toBeTrue();
      expect(component.form.get('name')?.disabled).toBeTrue();
      expect(component.form.get('description')?.disabled).toBeTrue();
      expect(component.form.get('value')?.disabled).toBeTrue();
    });

    it('should disable fields based on disabledOn configuration for update', () => {
      component.parameter = 'update';
      component.ngOnChanges({
        parameter: new SimpleChange('undef', 'update', false)
      });

      expect(component.form.get('id')?.disabled).toBeTrue();
      expect(component.form.get('name')?.disabled).toBeFalse();
    });

    it('should enable all non-restricted fields for create', () => {
      component.parameter = 'create';
      component.ngOnChanges({
        parameter: new SimpleChange('undef', 'create', false)
      });

      expect(component.form.get('id')?.disabled).toBeFalse();
      expect(component.form.get('name')?.disabled).toBeFalse();
      expect(component.form.get('description')?.disabled).toBeFalse();
      expect(component.form.get('value')?.disabled).toBeFalse();
    });

    it('should disable hidden fields', () => {
      component.parameter = 'delete';
      component.ngOnChanges({
        parameter: new SimpleChange('undef', 'delete', false)
      });

      expect(component.form.get('description')?.disabled).toBeTrue();
    });
  });

  describe('hide', () => {
    beforeEach(() => {
      component.config = mockConfig;
      component.ngOnChanges({
        config: new SimpleChange(null, mockConfig, true)
      });
      component.visible = true;
    });

    it('should set visible to false', () => {
      component.hide();
      expect(component.visible).toBeFalse();
    });

    it('should emit visibleChange event', () => {
      spyOn(component.visibleChange, 'emit');
      component.hide();
      expect(component.visibleChange.emit).toHaveBeenCalledWith(false);
    });

    it('should reset form', () => {
      component.form.get('name')?.setValue('Test');
      component.hide();
      expect(component.form.get('name')?.value).toBeNull();
    });

    it('should reset control states', () => {
      const nameControl = component.form.get('name');
      nameControl?.setValue('Test');
      nameControl?.markAsDirty();
      nameControl?.markAsTouched();

      component.hide();

      expect(nameControl?.pristine).toBeTrue();
      expect(nameControl?.untouched).toBeTrue();
    });
  });

  describe('submit', () => {
    beforeEach(() => {
      component.config = mockConfig;
      component.ngOnChanges({
        config: new SimpleChange(null, mockConfig, true)
      });
      component.visible = true;
    });

    it('should not submit when parameter is undef', () => {
      spyOn(component.actionPerformed, 'emit');
      component.parameter = 'undef';
      component.submit();
      expect(component.actionPerformed.emit).not.toHaveBeenCalled();
    });

    it('should emit actionPerformed for create action', () => {
      spyOn(component.actionPerformed, 'emit');
      component.parameter = 'create';
      component.form.patchValue({ name: 'New Item', value: 50 });

      component.submit();

      expect(component.actionPerformed.emit).toHaveBeenCalledWith({
        action: 'create',
        entity: jasmine.objectContaining({
          name: 'New Item',
          value: 50,
          mapped: true
        }),
        entityType: 'TestEntity'
      });
    });

    it('should emit actionPerformed for update action', () => {
      spyOn(component.actionPerformed, 'emit');
      component.parameter = 'update';
      component.entity = mockEntity;
      component.ngOnChanges({
        entity: new SimpleChange(null, mockEntity, false)
      });
      component.form.patchValue({ name: 'Updated Item' });

      component.submit();

      expect(component.actionPerformed.emit).toHaveBeenCalledWith({
        action: 'update',
        entity: jasmine.objectContaining({
          id: 1,
          name: 'Updated Item',
          mapped: true
        }),
        entityType: 'TestEntity'
      });
    });

    it('should emit actionPerformed for delete action', () => {
      spyOn(component.actionPerformed, 'emit');
      component.parameter = 'delete';
      component.entity = mockEntity;
      component.ngOnChanges({
        entity: new SimpleChange(null, mockEntity, false)
      });

      component.submit();

      expect(component.actionPerformed.emit).toHaveBeenCalledWith({
        action: 'delete',
        entity: jasmine.objectContaining({
          id: 1,
          mapped: true
        }),
        entityType: 'TestEntity'
      });
    });

    it('should apply mapOut function when provided', () => {
      spyOn(component.actionPerformed, 'emit');
      component.parameter = 'create';
      component.form.patchValue({ name: 'Test', value: 10 });

      component.submit();

      const emittedEntity = (component.actionPerformed.emit as jasmine.Spy).calls.mostRecent().args[0].entity;
      expect(emittedEntity.mapped).toBeTrue();
    });

    it('should hide panel after submit', () => {
      spyOn(component, 'hide');
      component.parameter = 'create';
      component.submit();
      expect(component.hide).toHaveBeenCalled();
    });

    it('should merge entity with form values', () => {
      spyOn(component.actionPerformed, 'emit');
      component.parameter = 'update';
      component.entity = { id: 1, name: 'Old', value: 100, extra: 'data' };
      component.ngOnChanges({
        entity: new SimpleChange(null, component.entity, false)
      });
      component.form.patchValue({ name: 'New' });

      component.submit();

      const emittedEntity = (component.actionPerformed.emit as jasmine.Spy).calls.mostRecent().args[0].entity;
      expect(emittedEntity.id).toBe(1);
      expect(emittedEntity.name).toBe('New');
      expect(emittedEntity.extra).toBe('data');
    });
  });

  describe('isHidden', () => {
    const testField: ManagePanelField = {
      name: 'test',
      label: 'Test',
      type: 'text',
      hiddenOn: ['delete']
    };

    it('should return true when field is hidden for current action', () => {
      component.parameter = 'delete';
      expect(component.isHidden(testField)).toBeTrue();
    });

    it('should return false when field is not hidden for current action', () => {
      component.parameter = 'create';
      expect(component.isHidden(testField)).toBeFalse();
    });

    it('should return false when field has no hiddenOn config', () => {
      const fieldWithoutHidden: ManagePanelField = {
        name: 'test',
        label: 'Test',
        type: 'text'
      };
      component.parameter = 'delete';
      expect(component.isHidden(fieldWithoutHidden)).toBeFalse();
    });
  });

  describe('actionLabel', () => {
    it('should return "Створити" for create action', () => {
      component.parameter = 'create';
      expect(component.actionLabel()).toBe('Створити');
    });

    it('should return "Оновити" for update action', () => {
      component.parameter = 'update';
      expect(component.actionLabel()).toBe('Оновити');
    });

    it('should return "Видалити" for delete action', () => {
      component.parameter = 'delete';
      expect(component.actionLabel()).toBe('Видалити');
    });

    it('should return "OK" for undefined action', () => {
      component.parameter = 'undef';
      expect(component.actionLabel()).toBe('OK');
    });
  });

  describe('header', () => {
    it('should return config title when provided', () => {
      component.config = mockConfig;
      expect(component.header()).toBe('Test Entity Management');
    });

    it('should return entityType when title is not provided', () => {
      const configWithoutTitle = { ...mockConfig, title: undefined };
      component.config = configWithoutTitle;
      expect(component.header()).toBe('TestEntity');
    });

    it('should return empty string when config is not set', () => {
      component.config = undefined as unknown as ManagePanelConfig;
      expect(component.header()).toBe('');
    });
  });

  describe('isDeleteMode', () => {
    it('should return true when parameter is delete', () => {
      component.parameter = 'delete';
      expect(component.isDeleteMode()).toBeTrue();
    });

    it('should return false when parameter is not delete', () => {
      component.parameter = 'create';
      expect(component.isDeleteMode()).toBeFalse();

      component.parameter = 'update';
      expect(component.isDeleteMode()).toBeFalse();

      component.parameter = 'undef';
      expect(component.isDeleteMode()).toBeFalse();
    });
  });

  describe('visibleFields', () => {
    beforeEach(() => {
      component.config = mockConfig;
    });

    it('should return all fields when no fields are hidden', () => {
      component.parameter = 'create';
      const visible = component.visibleFields();
      expect(visible.length).toBe(4);
    });

    it('should filter out hidden fields', () => {
      component.parameter = 'delete';
      const visible = component.visibleFields();
      expect(visible.length).toBe(3);
      expect(visible.find(f => f.name === 'description')).toBeUndefined();
    });

    it('should return empty array when config is not set', () => {
      component.config = undefined as unknown as ManagePanelConfig;
      expect(component.visibleFields().length).toBe(0);
    });
  });

  describe('onDialogShow', () => {
    beforeEach(() => {
      component.config = mockConfig;
      component.ngOnChanges({
        config: new SimpleChange(null, mockConfig, true)
      });
    });

    it('should not focus input in delete mode', () => {
      component.parameter = 'delete';
      spyOn(component as unknown as { focusFirstInput: () => void }, 'focusFirstInput');
      
      component.onDialogShow();
      
      expect(component['focusFirstInput']).not.toHaveBeenCalled();
    });

    it('should focus first input in non-delete modes', () => {
      component.parameter = 'create';
      spyOn(component as unknown as { focusFirstInput: () => void }, 'focusFirstInput');
      
      component.onDialogShow();
      
      expect(component['focusFirstInput']).toHaveBeenCalled();
    });
  });

  describe('Integration scenarios', () => {
    beforeEach(() => {
      component.config = mockConfig;
      component.ngOnChanges({
        config: new SimpleChange(null, mockConfig, true)
      });
    });

    it('should handle complete create flow', () => {
      spyOn(component.actionPerformed, 'emit');
      
      component.parameter = 'create';
      component.visible = true;
      component.ngOnChanges({
        parameter: new SimpleChange('undef', 'create', false),
        visible: new SimpleChange(false, true, false)
      });

      component.form.patchValue({
        name: 'New Entity',
        description: 'Description',
        value: 200
      });

      component.submit();

      expect(component.actionPerformed.emit).toHaveBeenCalledWith({
        action: 'create',
        entity: jasmine.objectContaining({
          name: 'New Entity',
          description: 'Description',
          value: 200
        }),
        entityType: 'TestEntity'
      });
      expect(component.visible).toBeFalse();
    });

    it('should handle complete update flow', () => {
      spyOn(component.actionPerformed, 'emit');
      
      component.parameter = 'update';
      component.entity = mockEntity;
      component.visible = true;
      
      component.ngOnChanges({
        entity: new SimpleChange(null, mockEntity, false),
        parameter: new SimpleChange('undef', 'update', false),
        visible: new SimpleChange(false, true, false)
      });

      component.form.patchValue({ name: 'Updated Name' });
      component.submit();

      expect(component.actionPerformed.emit).toHaveBeenCalledWith({
        action: 'update',
        entity: jasmine.objectContaining({
          id: 1,
          name: 'Updated Name'
        }),
        entityType: 'TestEntity'
      });
    });

    it('should handle complete delete flow', () => {
      spyOn(component.actionPerformed, 'emit');
      
      component.parameter = 'delete';
      component.entity = mockEntity;
      component.visible = true;
      
      component.ngOnChanges({
        entity: new SimpleChange(null, mockEntity, false),
        parameter: new SimpleChange('undef', 'delete', false),
        visible: new SimpleChange(false, true, false)
      });

      component.submit();

      expect(component.actionPerformed.emit).toHaveBeenCalledWith({
        action: 'delete',
        entity: jasmine.objectContaining({ id: 1 }),
        entityType: 'TestEntity'
      });
    });

    it('should validate required fields on submit', () => {
      component.parameter = 'create';
      component.visible = true;
      
      component.ngOnChanges({
        parameter: new SimpleChange('undef', 'create', false),
        visible: new SimpleChange(false, true, false)
      });

      // Очищаємо required поля
      component.form.patchValue({
        name: '',
        value: null
      });

      // Don't fill required fields
      expect(component.form.valid).toBeFalse();
      expect(component.form.get('name')?.errors?.['required']).toBeTrue();
      expect(component.form.get('value')?.errors?.['required']).toBeTrue();
    });
  });
});