import { Component, computed, input, output } from '@angular/core';
import { IonItem, IonItemGroup, IonList, IonSelect, IonSelectOption } from '@ionic/angular';
import { periodOptions, periodQueryOf, periodValue } from './score.format';
import { ScorePeriodDto, ScorePeriodQuery, ScorePeriodsDto } from './score.models';

/**
 * The years and stages a score may be totalled over (the web's score-period-select), as one row
 * with an action sheet. Hidden while there is only the current year: a choice of one is noise.
 */
@Component({
  selector: 'app-score-period',
  imports: [IonList, IonItemGroup, IonItem, IonSelect, IonSelectOption],
  template: `
    @if (options().length) {
      <ion-list [inset]="true">
        <ion-item-group>
          <ion-item>
            <ion-select
              data-testid="score-period"
              label="Період"
              interface="action-sheet"
              cancelText="Скасувати"
              [interfaceOptions]="{ header: 'Період' }"
              [value]="value()"
              (ionChange)="pick($event)"
            >
              @for (option of options(); track option.value) {
                <ion-select-option [value]="option.value">{{ option.label }}</ion-select-option>
              }
            </ion-select>
          </ion-item>
        </ion-item-group>
      </ion-list>
    }
  `,
})
export class ScorePeriodSelect {
  readonly periods = input.required<ScorePeriodsDto>();
  readonly period = input.required<ScorePeriodDto>();
  readonly periodChange = output<ScorePeriodQuery>();

  protected readonly options = computed(() => periodOptions(this.periods()));
  protected readonly value = computed(() => periodValue(this.period()));

  protected pick(change: Event): void {
    const value = (change as CustomEvent<{ value?: string }>).detail.value;
    if (value && value !== this.value()) this.periodChange.emit(periodQueryOf(value));
  }
}
