import { ChangeDetectionStrategy, Component, computed, input, model } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from '@openng/optimus-ui/button';
import { SelectModule } from '@openng/optimus-ui/select';
import { InputTextModule } from '@openng/optimus-ui/inputtext';
import { ToggleSwitchModule } from '@openng/optimus-ui/toggleswitch';
import { CardExtraFieldDto, CardFieldMode, DEFAULT_CARD_FIELDS, StandardCardField } from '../../../../kurinModule/models/card-fields.dto';
import { KraiCardFieldsDocument } from '../../../models/krai-profile.dto';

type StandardField = StandardCardField;

const MODE_LABELS: Record<CardFieldMode, string> = {
  required: 'Обовʼязкове',
  optional: 'Необовʼязкове',
  hidden: 'Приховане'
};

/**
 * Картка члена в профілі краю: які з її полів обовʼязкові, які можна лишити порожніми, яких у
 * краї немає взагалі, і які поля край додає від себе. Значення кожного доданого поля тримається
 * під його ключем, тож ключ після збереження не міняють.
 */
@Component({
  selector: 'app-krai-card-fields',
  imports: [FormsModule, ButtonModule, SelectModule, InputTextModule, ToggleSwitchModule],
  templateUrl: './krai-card-fields.html',
  styleUrl: '../krai-settings.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class KraiCardFieldsComponent {
  readonly value = model<KraiCardFieldsDocument | undefined>(undefined);
  readonly disabled = input(false);

  readonly standardFields: { field: StandardField; label: string }[] = [
    { field: 'middleName', label: 'По батькові' },
    { field: 'phoneNumber', label: 'Номер телефону' },
    { field: 'address', label: 'Адреса' },
    { field: 'school', label: 'Школа' }
  ];
  readonly modeOptions = (Object.keys(MODE_LABELS) as CardFieldMode[]).map(mode => ({ mode, label: MODE_LABELS[mode] }));
  readonly typeOptions = [
    { type: 'text', label: 'Текст' },
    { type: 'date', label: 'Дата' }
  ];

  readonly extra = computed(() => this.value()?.extra ?? []);

  mode(field: StandardField): CardFieldMode | null {
    return this.value()?.fields?.[field] ?? null;
  }

  defaultLabel(field: StandardField): string {
    return `Як за замовчуванням (${MODE_LABELS[DEFAULT_CARD_FIELDS.fields[field]].toLowerCase()})`;
  }

  setMode(field: StandardField, mode: CardFieldMode | null): void {
    const fields = { ...this.value()?.fields };
    if (mode) {
      fields[field] = mode;
    } else {
      delete fields[field];
    }
    this.emit(fields, this.extra());
  }

  addField(): void {
    const taken = new Set(this.extra().map(field => field.key));
    let index = this.extra().length + 1;
    while (taken.has(`field${index}`)) {
      index++;
    }
    this.emit(this.value()?.fields, [...this.extra(), { key: `field${index}`, label: '', type: 'text', required: false }]);
  }

  updateField(index: number, change: Partial<CardExtraFieldDto>): void {
    this.emit(this.value()?.fields, this.extra().map((field, at) => at === index ? { ...field, ...change } : field));
  }

  removeField(index: number): void {
    this.emit(this.value()?.fields, this.extra().filter((_, at) => at !== index));
  }

  private emit(fields: Record<string, CardFieldMode> | undefined, extra: CardExtraFieldDto[]): void {
    const hasFields = !!fields && Object.keys(fields).length > 0;
    this.value.set(hasFields || extra.length ? { fields: hasFields ? fields : undefined, extra: extra.length ? extra : undefined } : undefined);
  }
}

/** Whether two card sections say the same, whatever order their keys came in. */
export function sameCardFields(left: KraiCardFieldsDocument | undefined, right: KraiCardFieldsDocument | undefined): boolean {
  const modes = (doc: KraiCardFieldsDocument | undefined) =>
    Object.entries(doc?.fields ?? {}).sort(([a], [b]) => a.localeCompare(b)).map(([field, mode]) => `${field}=${mode}`).join(';');
  const extra = (doc: KraiCardFieldsDocument | undefined) =>
    (doc?.extra ?? []).map(field => [field.key, field.label, field.type, !!field.required].join('|')).join(';');
  return modes(left) === modes(right) && extra(left) === extra(right);
}
