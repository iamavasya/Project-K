import {
  Component,
  ElementRef,
  afterRenderEffect,
  computed,
  input,
  viewChild,
} from '@angular/core';
import { PlanningSessionDto } from './planning.models';
import { optimalDays, stripAxis, stripBar, stripMonths } from './planning.labels';

/** One column per day; the names stay put on the left while the days scroll under them. */
const DAY = 20;
const NAME = 124;

/**
 * The web's «Графік зайнятості» (a Chart.js timeline in planning-detail) made for a phone: the
 * search window as a strip of days that scrolls sideways, the camp's row on top and each person's
 * busy ranges below, the chosen days shaded through every row. It opens on the camp's days.
 */
@Component({
  selector: 'app-availability-strip',
  styles: `
    :host {
      display: block;
    }
    .panel {
      --strip-bg: var(--ion-item-background, var(--lk-paper));
      margin: 0 16px;
      padding: 6px 0 10px;
      background: var(--strip-bg);
      border: 1px solid var(--lk-line);
      border-radius: 12px;
      overflow: hidden;
    }
    :host-context(.ios) .panel {
      margin: 0 20px;
      padding: 10px 0 14px;
      border: 0;
      border-radius: 28px;
    }
    .scroller {
      overflow-x: auto;
      overscroll-behavior-x: contain;
      scrollbar-width: none;
    }
    .scroller::-webkit-scrollbar {
      display: none;
    }
    .grid {
      position: relative;
    }
    .row {
      position: relative;
      display: flex;
      height: 30px;
    }
    .row.months,
    .row.days {
      height: 20px;
    }
    .name {
      position: sticky;
      left: 0;
      z-index: 3;
      display: flex;
      align-items: center;
      flex: none;
      box-sizing: border-box;
      padding: 0 8px 0 16px;
      background: var(--strip-bg);
      font-size: 13px;
      font-weight: 600;
      color: var(--lk-ink);
      border-right: 1px solid var(--lk-line);
    }
    .name span {
      min-width: 0;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }
    .name.camp {
      color: var(--lk-primary-700);
    }
    .track {
      position: relative;
      flex: none;
      border-top: 1px solid var(--lk-line);
    }
    .months .track,
    .days .track {
      border-top: 0;
    }
    .month {
      position: absolute;
      top: 0;
      bottom: 0;
      border-left: 1px solid var(--lk-line);
    }
    .month span {
      position: sticky;
      left: ${NAME + 4}px;
      display: inline-block;
      max-width: 100%;
      box-sizing: border-box;
      padding: 0 4px;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
      font-size: 12px;
      font-weight: 700;
      line-height: 20px;
      color: var(--lk-ink);
    }
    .day {
      position: absolute;
      top: 0;
      width: ${DAY}px;
      text-align: center;
      font-size: 10px;
      line-height: 20px;
      color: var(--lk-muted);
      font-variant-numeric: tabular-nums;
    }
    .day.weekend {
      color: var(--lk-faint);
    }
    .shade {
      position: absolute;
      top: 0;
      bottom: 0;
      width: ${DAY}px;
      background: var(--lk-line);
      opacity: 0.35;
    }
    .band {
      position: absolute;
      top: 0;
      bottom: 0;
      background: var(--lk-primary-50);
      border-inline: 1px solid var(--lk-primary);
    }
    .bar {
      position: absolute;
      top: 8px;
      height: 14px;
      border-radius: 4px;
      background: var(--lk-danger);
    }
    .bar.camp {
      top: 6px;
      height: 18px;
      background: var(--lk-primary);
    }
    .legend {
      display: flex;
      flex-wrap: wrap;
      gap: 4px 16px;
      padding: 10px 16px 0;
      font-size: 13px;
      color: var(--lk-muted);
    }
    :host-context(.ios) .legend {
      padding-inline: 20px;
    }
    .legend i {
      display: inline-block;
      width: 12px;
      height: 12px;
      margin-right: 6px;
      border-radius: 3px;
      vertical-align: -1px;
      background: var(--lk-danger);
    }
    .legend i.camp {
      background: var(--lk-primary);
    }
  `,
  template: `
    <div class="panel">
      <div class="scroller" #scroller data-testid="strip">
        <div class="grid" [style.width.px]="width()" role="img" [attr.aria-label]="summary()">
          <div class="body-layer" aria-hidden="true">
            @for (day of weekends(); track day) {
              <div
                class="shade"
                [style.left.px]="name + day * dayWidth"
                [style.top.px]="40"
                [style.bottom.px]="0"
              ></div>
            }
            @if (band(); as b) {
              <div
                class="band"
                [style.left.px]="name + b.left * dayWidth"
                [style.width.px]="b.width * dayWidth"
                [style.top.px]="40"
              ></div>
            }
          </div>
          <div class="row months" aria-hidden="true">
            <div class="name" [style.width.px]="name"></div>
            <div class="track" [style.width.px]="trackWidth()">
              @for (month of months(); track month.from) {
                <div
                  class="month"
                  [style.left.px]="month.from * dayWidth"
                  [style.width.px]="month.span * dayWidth"
                >
                  <span>{{ month.label }}</span>
                </div>
              }
            </div>
          </div>
          <div class="row days" aria-hidden="true">
            <div class="name" [style.width.px]="name"></div>
            <div class="track" [style.width.px]="trackWidth()">
              @for (day of axis().days; track $index) {
                <span
                  class="day"
                  [class.weekend]="day.getDay() === 0 || day.getDay() === 6"
                  [style.left.px]="$index * dayWidth"
                >{{ day.getDate() }}</span>
              }
            </div>
          </div>
          <div class="row" data-testid="strip-camp">
            <div class="name camp" [style.width.px]="name"><span>Табір</span></div>
            <div class="track" [style.width.px]="trackWidth()">
              @if (band(); as b) {
                <div
                  class="bar camp"
                  [style.left.px]="b.left * dayWidth"
                  [style.width.px]="b.width * dayWidth"
                ></div>
              }
            </div>
          </div>
          @for (row of rows(); track $index) {
            <div class="row" data-testid="strip-row">
              <div class="name" [style.width.px]="name">
                <span>{{ row.name }}</span>
              </div>
              <div class="track" [style.width.px]="trackWidth()">
                @for (bar of row.bars; track $index) {
                  <div
                    class="bar"
                    [style.left.px]="bar.left * dayWidth"
                    [style.width.px]="bar.width * dayWidth"
                  ></div>
                }
              </div>
            </div>
          }
        </div>
      </div>
      <div class="legend" aria-hidden="true">
        @if (band()) {
          <span><i class="camp"></i>Оптимальний час</span>
        }
        <span><i></i>Зайнятий</span>
      </div>
    </div>
  `,
})
export class AvailabilityStrip {
  readonly session = input.required<PlanningSessionDto>();

  protected readonly name = NAME;
  protected readonly dayWidth = DAY;
  private readonly scroller = viewChild.required<ElementRef<HTMLElement>>('scroller');

  protected readonly axis = computed(() => stripAxis(this.session()));
  protected readonly trackWidth = computed(() => this.axis().days.length * DAY);
  protected readonly width = computed(() => NAME + this.trackWidth());
  protected readonly months = computed(() => stripMonths(this.axis()));
  protected readonly weekends = computed(() =>
    this.axis()
      .days.map((day, index) => (day.getDay() === 0 || day.getDay() === 6 ? index : -1))
      .filter((index) => index >= 0),
  );
  protected readonly band = computed(() => {
    const s = this.session();
    if (!s.isCalculated || !s.optimalStartDate || !s.optimalEndDate) return null;
    return stripBar(this.axis(), { start: s.optimalStartDate, end: s.optimalEndDate });
  });
  protected readonly rows = computed(() =>
    this.session().participants.map((p) => ({
      name: p.fullName,
      bars: p.busyRanges.map((range) => stripBar(this.axis(), range)).filter((bar) => bar !== null),
    })),
  );
  protected readonly summary = computed(() => {
    const busy = this.rows().filter((row) => row.bars.length).length;
    const camp = optimalDays(this.session()) ? 'Оптимальні дні позначено. ' : '';
    return `Графік зайнятості. ${camp}Зайняті у вікні: ${busy} з ${this.rows().length}.`;
  });

  constructor() {
    // Open on the camp's days (a little before them), as the web's chart frames the window.
    afterRenderEffect(() => {
      const band = this.band();
      const el = this.scroller().nativeElement;
      el.scrollLeft = band ? Math.max(0, (band.left - 2) * DAY) : 0;
    });
  }
}
