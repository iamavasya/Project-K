import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SelectModule } from '@openng/optimus-ui/select';
import { periodQueryOf, periodValue } from '../../functions/score-format.function';
import { ScorePeriodDto, ScorePeriodQuery, ScorePeriodsDto } from '../../models/score.dto';

/**
 * The years and stages a score may be totalled over, as one select. Hidden while there is only the
 * current year to choose: a choice of one is noise.
 */
@Component({
  selector: 'app-score-period-select',
  imports: [FormsModule, SelectModule],
  template: `
    @if (options().length > 1) {
      <p-select
        [options]="options()"
        [ngModel]="value()"
        (ngModelChange)="periodChange.emit(periodQueryOf($event))"
        optionLabel="label"
        optionValue="value"
        optionGroupLabel="label"
        optionGroupChildren="items"
        [group]="hasStages()"
        appendTo="body"
        ariaLabel="Період"
        styleClass="score-period" />
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ScorePeriodSelectComponent {
  readonly periods = input.required<ScorePeriodsDto>();
  readonly period = input.required<ScorePeriodDto>();
  readonly periodChange = output<ScorePeriodQuery>();

  readonly periodQueryOf = periodQueryOf;

  readonly value = computed(() => periodValue(this.period()));
  readonly hasStages = computed(() => this.periods().stages.length > 0);

  readonly options = computed(() => {
    const years = this.periods().years.map(toOption);
    const stages = this.periods().stages.map(toOption);
    if (!stages.length) {
      return years;
    }
    return [
      { label: 'Пластовий рік', items: years },
      { label: 'Етапи', items: stages }
    ];
  });
}

function toOption(period: ScorePeriodDto): { label: string; value: string } {
  return { label: period.label, value: periodValue(period) };
}
