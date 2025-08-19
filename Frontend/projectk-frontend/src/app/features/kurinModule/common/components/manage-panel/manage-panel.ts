/* eslint-disable @typescript-eslint/no-explicit-any */
import { Component, ElementRef, EventEmitter, Input, OnChanges, Output, QueryList, SimpleChanges, ViewChildren } from '@angular/core';
import { DialogModule } from 'primeng/dialog';
import { CommonModule, TitleCasePipe } from '@angular/common';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { FormControl, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';

export type ManageAction = 'create' | 'update' | 'delete';
export type ManageFieldType = 'text' | 'number' | 'textarea';

export interface ManagePanelField {
  name: string;
  label: string;
  type: ManageFieldType;
  placeholder?: string;
  required?: boolean;
  disabledOn?: ManageAction[];
  hiddenOn?: ManageAction[];
}

export interface ManagePanelConfig {
  entityType: string;
  title?: string;
  fields: ManagePanelField[];
  createFactory?: () => any;
  mapOut?(raw: any): any;
}

@Component({
  selector: 'app-manage-panel',
  imports: [DialogModule, CommonModule, InputTextModule, ButtonModule, FormsModule, ReactiveFormsModule, TitleCasePipe],
  templateUrl: './manage-panel.html',
  styleUrl: './manage-panel.scss'
})
export class ManagePanel implements OnChanges {
  @Input() visible = false;
  @Input() parameter: ManageAction | 'undef' = 'undef';
  @Input() entity: any | null = null;
  @Input() config!: ManagePanelConfig;

  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() actionPerformed = new EventEmitter<{action: ManageAction; entity: any; entityType: string}>();

  @ViewChildren('autoField') autoFields!: QueryList<ElementRef>;

  form: FormGroup = new FormGroup({});
  ready = false;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['config']) {
      this.buildForm();
    } else if (changes['entity']) {
      this.patchEntity();
    }
    if (changes['parameter']) {
      this.applyStatePerAction();
      if (this.parameter === 'create') {
        this.resetControlsState();
      }
    }
    if (changes['visible'] && this.visible) {
      if (this.parameter === 'create') {
        this.buildForm(); // новий чистий об'єкт
      } else {
        // інші дії: просто пропатчити актуальне entity
        this.patchEntity();
        this.applyStatePerAction();
      }
      this.resetControlsState();
    }
  }
  
  onDialogShow(): void {
    if(!this.isDeleteMode()) {
      this.focusFirstInput();
    }
  }

  private focusFirstInput(): void {
    const target = this.autoFields?.find(ref => {
      const el = ref.nativeElement as HTMLElement;
      const disabled = (el as HTMLInputElement).disabled;
      const hidden = el.offsetParent === null;
      return !disabled && !hidden;
    })?.nativeElement;

    if (target) {
      setTimeout(() => target.focus(), 0);
    }
  }

  private resetControlsState(): void {
    if (!this.form) return;
    Object.values(this.form.controls).forEach(c => {
      c.markAsPristine();
      c.markAsUntouched();
      c.updateValueAndValidity({ onlySelf: true, emitEvent: false });
    });
  }

  private buildForm(): void {
    if (!this.config) return;
    this.ready = false;
    const base = this.entity ?? this.config.createFactory?.() ?? {};
    const group: Record<string, FormControl> = {};

    this.config.fields.forEach(field => {
      group[field.name] = new FormControl(
        base[field.name] ?? null,
        field.required ? [Validators.required] : []
      );
    });

    this.form = new FormGroup(group);
    this.applyStatePerAction();
    this.ready = true;
  }

  private patchEntity(): void {
    if (!this.config || !this.form) return;
    const src = this.entity ?? this.config.createFactory?.() ?? {};
    this.config.fields.forEach(f => {
      if (this.form.get(f.name)) {
        this.form.get(f.name)!.setValue(src[f.name] ?? null, { emitEvent: false });
      }
    });
  }

  private applyStatePerAction(): void {
    if (!this.config || !this.form) return;
    const action = this.parameter as ManageAction;

    this.config.fields.forEach(f => {
      const ctrl = this.form.get(f.name);
      if (!ctrl) return;

      const hidden = !!f.hiddenOn && f.hiddenOn.includes(action);
      const shouldDisableBecauseOfAction =
        action === 'delete' ||
        (f.disabledOn && f.disabledOn.includes(action));

      // Якщо поле сховане — теж disable, щоб валідатор не блокував submit
      const needDisable = hidden || shouldDisableBecauseOfAction;

      if (needDisable && !ctrl.disabled) {
        ctrl.disable({ emitEvent: false });
      } else if (!needDisable && ctrl.disabled) {
        ctrl.enable({ emitEvent: false });
      }
    });
  }

  hide(): void {
    this.visible = false;
    this.visibleChange.emit(this.visible);

    if (this.form) {
      this.form.reset();
      this.resetControlsState();
    }
  }

  submit(): void {
    if (this.parameter === 'undef') return;
    const action = this.parameter as ManageAction;

    let raw = {
      ...(this.entity ?? this.config.createFactory?.() ?? {}),
      ...this.form.getRawValue()
    };

    if (this.config.mapOut) {
      raw = this.config.mapOut(raw);
    }

    this.actionPerformed.emit({
      action,
      entity: raw,
      entityType: this.config.entityType
    });
    this.hide();
  }

  isHidden(field: ManagePanelField): boolean {
    const action = this.parameter as ManageAction;
    return !!field.hiddenOn && field.hiddenOn.includes(action);
  }

  actionLabel(): string {
    switch (this.parameter) {
      case 'create': return 'Створити';
      case 'update': return 'Оновити';
      case 'delete': return 'Видалити';
      default: return 'OK';
    }
  }

  header(): string {
    return this.config?.title || (this.config?.entityType ?? '');
  }

  isDeleteMode(): boolean {
    return this.parameter === 'delete';
  }

  visibleFields(): ManagePanelField[] {
    if (!this.config) return [];
    return this.config.fields.filter(f => !this.isHidden(f));
  }
}
