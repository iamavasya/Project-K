import { Component, NgZone, computed, effect, inject, input, signal } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { IonCard, IonCardContent } from '@ionic/angular';
import { shortDate, utcDate } from './kurin.labels';
import { ProbeProjectDto, ThirdProbeDto } from './kurin.models';
import { MarkdownBlock, hasContent, parseProjectMarkdown } from './project-markdown';
import { areaOf, countdownParts, dayWord, reachLabels, thirdProbeChecklist, youthEndsOn } from './third-probe';

const IMAGE_SCHEME = 'project-image:';

const TEXTS: readonly { field: 'goal' | 'plan' | 'results'; label: string }[] = [
  { field: 'goal', label: 'Ціль' },
  { field: 'plan', label: 'Підготовка' },
  { field: 'results', label: 'Підсумки' },
];

interface ProjectView {
  project: ProbeProjectDto;
  area: { letter: string; label: string } | null;
  facts: { label: string; value: string }[];
  texts: { label: string; blocks: MarkdownBlock[] }[];
}

/**
 * The «Третя проба» tab on the phone (web third-probe): the countdown to the 18th birthday, what
 * is done, the camp with its посвідка, the вмілості, five projects and the spare, and the résumé.
 * Read here; written on the web, where the editors and uploads live.
 */
@Component({
  selector: 'app-third-probe',
  imports: [NgTemplateOutlet, IonCard, IonCardContent],
  styles: `
    h2 { margin: 0 0 4px; font-size: 18px; font-weight: 700; color: var(--lk-ink); }
    h3 { margin: 0; font-size: 16px; font-weight: 700; color: var(--lk-ink); overflow-wrap: anywhere; }
    h4 { margin: 10px 0 2px; font-size: 12px; font-weight: 700; letter-spacing: 0.05em; text-transform: uppercase; color: var(--lk-muted); }
    .meta { margin: 4px 0 0; font-size: 13px; color: var(--lk-muted); }
    .meta strong, .line { color: var(--lk-ink); }
    .line { margin: 4px 0 0; font-size: 14px; }
    .digits { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 6px; margin-top: 8px; }
    .unit { display: flex; flex-direction: column; align-items: center; padding: 8px 2px; border: 1px solid var(--lk-line); border-radius: 12px; }
    .value { font-size: 26px; font-weight: 800; line-height: 1.1; font-variant-numeric: tabular-nums; color: var(--lk-primary); }
    .soon .value { color: var(--lk-accent-500, var(--lk-primary)); }
    .name { font-size: 11px; color: var(--lk-muted); }
    .checks { display: flex; flex-direction: column; gap: 6px; margin-top: 6px; }
    .check { display: flex; gap: 8px; align-items: baseline; font-size: 14px; color: var(--lk-ink); }
    .check .mark { width: 16px; color: var(--lk-faint); }
    .check.done .mark { color: var(--lk-primary); }
    .head { display: flex; flex-wrap: wrap; align-items: center; gap: 8px; }
    .badge { border: 1px solid var(--lk-line); border-radius: 999px; padding: 1px 8px; font-size: 11px; font-weight: 600; color: var(--lk-muted); }
    .badge.ok { border-color: var(--lk-primary); color: var(--lk-primary); }
    .letter { border: 1px solid var(--lk-primary); color: var(--lk-primary); border-radius: 8px; width: 22px; height: 22px;
      display: inline-flex; align-items: center; justify-content: center; font-size: 12px; font-weight: 700; flex: none; }
    .facts { display: grid; grid-template-columns: max-content minmax(0, 1fr); gap: 2px 10px; margin: 8px 0 0; font-size: 13px; }
    .facts dt { color: var(--lk-muted); }
    .facts dd { margin: 0; color: var(--lk-ink); overflow-wrap: anywhere; }
    .spare { border: 1px dashed var(--lk-line); }
    .vml { display: flex; gap: 10px; align-items: center; margin-top: 8px; }
    .vml img { width: 36px; height: 36px; object-fit: contain; flex: none; }
    .certificate { display: block; max-width: 100%; max-height: 220px; margin-top: 8px; border: 1px solid var(--lk-line); border-radius: 8px; }
    .text { color: var(--lk-ink); font-size: 15px; line-height: 1.5; overflow-wrap: anywhere; }
    .text p { margin: 0; min-height: 0.75em; }
    .text .h { font-weight: 700; margin-top: 6px; }
    .text .li { position: relative; }
    .text .mk { display: inline-block; width: 20px; margin-left: -20px; color: var(--lk-muted); }
    .text blockquote { margin: 4px 0; padding-left: 10px; border-left: 1px solid var(--lk-line); color: var(--lk-muted); }
    .text pre, .text code { font-family: ui-monospace, Menlo, Consolas, monospace; font-size: 0.875em; border: 1px solid var(--lk-line); border-radius: 8px; }
    .text code { padding: 0 4px; }
    .text pre { padding: 8px 10px; white-space: pre-wrap; }
    .text a { color: var(--lk-primary); }
    .text img { display: block; max-width: 100%; max-height: 320px; margin: 8px 0; border: 1px solid var(--lk-line); border-radius: 8px; }
    .b { font-weight: 600; }
    .i { font-style: italic; }
    .s { text-decoration: line-through; }
  `,
  template: `
    @if (deadline(); as end) {
      <ion-card data-testid="third-probe-countdown" [class.soon]="parts().days < 90">
        <ion-card-content>
          @if (!parts().isOver) {
            <h2>До {{ data().limits.youthEndsAtAge }}-річчя</h2>
            <p class="meta">кінця юнацтва й третьої проби · {{ endLabel() }}</p>
            <div class="digits" role="timer">
              <div class="unit"><span class="value">{{ parts().days }}</span><span class="name">{{ days() }}</span></div>
              <div class="unit"><span class="value">{{ pad(parts().hours) }}</span><span class="name">год</span></div>
              <div class="unit"><span class="value">{{ pad(parts().minutes) }}</span><span class="name">хв</span></div>
              <div class="unit"><span class="value">{{ pad(parts().seconds) }}</span><span class="name">с</span></div>
            </div>
          } @else {
            <h2>Юнацтво закінчилося</h2>
            <p class="meta">{{ endLabel() }}</p>
          }
        </ion-card-content>
      </ion-card>
    }

    <ion-card data-testid="third-probe-checklist">
      <ion-card-content>
        <h2>Що вже є</h2>
        <div class="checks">
          @for (item of checklist(); track item.label) {
            <div class="check" [class.done]="item.done">
              <span class="mark">{{ item.done ? '✓' : '–' }}</span>
              <span><strong>{{ item.label }}</strong> <span class="meta">· {{ item.detail }}</span></span>
            </div>
          }
        </div>
        @if (data().canEdit) {
          <p class="meta">Писати проєкти, табір і резюме можна у вебверсії Лілейки.</p>
        }
      </ion-card-content>
    </ion-card>

    @if (data().file; as file) {
      <ion-card>
        <ion-card-content>
          <h2>Відкриття й табір</h2>
          <dl class="facts">
            <dt>Відкрито</dt><dd>{{ day(file.openedOn) ?? 'ще не відкрито' }}</dd>
            @if (file.mentors) { <dt>Наставники</dt><dd>{{ file.mentors }}</dd> }
            <dt>Конференція</dt><dd>{{ day(file.conferenceOn) ?? 'ще не відбулася' }}@if (file.conferenceConfirmedAtUtc) { ✓ }</dd>
            <dt>Табір</dt><dd>{{ file.campName ?? 'ще не вказано' }}@if (file.campConfirmedAtUtc) { ✓ }</dd>
          </dl>
          @if (file.campCertificate; as cert) {
            @if (cert.contentType === 'application/pdf') {
              <p class="line"><a [href]="cert.url" target="_blank" rel="noopener noreferrer">Посвідка (PDF)</a></p>
            } @else {
              <a [href]="cert.url" target="_blank" rel="noopener noreferrer"><img class="certificate" [src]="cert.url" alt="Посвідка табору" /></a>
            }
          }
        </ion-card-content>
      </ion-card>
    }

    <ion-card data-testid="third-probe-badges">
      <ion-card-content>
        <h2>Вмілості</h2>
        <p class="meta">під час проби {{ during().length }} з {{ data().limits.requiredBadges }}, одна — з тіловиховання</p>
        @for (badge of during(); track badge.badgeId) {
          <div class="vml">
            @if (badge.imagePath) { <img [src]="badge.imagePath" alt="" loading="lazy" /> }
            <div>
              <p class="line"><strong>{{ badge.title }}</strong></p>
              @if (badge.badgeId === data().file?.physicalBadgeId) { <span class="badge ok">Тіловиховання</span> }
            </div>
          </div>
        }
        @if (earlier().length) {
          <p class="meta">До проби: {{ earlierTitles() }}</p>
        }
      </ion-card-content>
    </ion-card>

    @for (item of projects(); track item.project.projectKey) {
      <ion-card data-testid="probe-project" [class.spare]="item.project.isSpare">
        <ion-card-content>
          <div class="head">
            @if (item.area) { <span class="letter">{{ item.area.letter }}</span> }
            <h3>{{ item.project.title }}</h3>
            @if (item.project.isSpare) { <span class="badge">Запасний</span> }
            @if (item.project.isShared) { <span class="badge">Спільний</span> }
            @if (item.project.completedAtUtc) { <span class="badge ok">Виконано</span> } @else if (item.project.approvedAtUtc) { <span class="badge ok">Дозвіл є</span> }
          </div>
          @if (item.facts.length) {
            <dl class="facts">
              @for (fact of item.facts; track fact.label) { <dt>{{ fact.label }}</dt><dd>{{ fact.value }}</dd> }
            </dl>
          }
          @for (text of item.texts; track text.label) {
            <h4>{{ text.label }}</h4>
            <div class="text">
              @for (block of text.blocks; track $index) {
                @switch (block.kind) {
                  @case ('heading') { <p class="h"><ng-container *ngTemplateOutlet="runsTpl; context: { $implicit: block.runs }" /></p> }
                  @case ('item') {
                    <p class="li" [style.padding-left.px]="20 + block.indent * 20">
                      <span class="mk" aria-hidden="true">{{ marker(block) }}</span>
                      <ng-container *ngTemplateOutlet="runsTpl; context: { $implicit: block.runs }" />
                    </p>
                  }
                  @case ('quote') { <blockquote><ng-container *ngTemplateOutlet="runsTpl; context: { $implicit: block.runs }" /></blockquote> }
                  @case ('code') { <pre>{{ block.text }}</pre> }
                  @default { <p><ng-container *ngTemplateOutlet="runsTpl; context: { $implicit: block.runs }" /></p> }
                }
              }
            </div>
          }
          @if (item.project.coordinatorComment) {
            <p class="meta">«{{ item.project.coordinatorComment }}» — координатор</p>
          }
        </ion-card-content>
      </ion-card>
    }

    @if (resume().length) {
      <ion-card>
        <ion-card-content>
          <h2>Пластове резюме</h2>
          <div class="text">
            @for (block of resume(); track $index) {
              @switch (block.kind) {
                @case ('heading') { <p class="h"><ng-container *ngTemplateOutlet="runsTpl; context: { $implicit: block.runs }" /></p> }
                @case ('item') {
                  <p class="li" [style.padding-left.px]="20 + block.indent * 20"><span class="mk" aria-hidden="true">{{ marker(block) }}</span><ng-container *ngTemplateOutlet="runsTpl; context: { $implicit: block.runs }" /></p>
                }
                @case ('code') { <pre>{{ block.text }}</pre> }
                @default { <p><ng-container *ngTemplateOutlet="runsTpl; context: { $implicit: block.runs }" /></p> }
              }
            }
          </div>
        </ion-card-content>
      </ion-card>
    }

    <ng-template #runsTpl let-runs>
      @for (run of runs; track $index) {
        @if (run.image) {
          <img [src]="run.image" [alt]="run.text || 'Зображення проєкту'" loading="lazy" />
        } @else if (run.link) {
          <a [href]="run.link" target="_blank" rel="noopener noreferrer" [class.b]="run.bold" [class.i]="run.italic" [class.s]="run.strike">{{ run.text }}</a>
        } @else if (run.code) {
          <code>{{ run.text }}</code>
        } @else {
          <span [class.b]="run.bold" [class.i]="run.italic" [class.s]="run.strike">{{ run.text }}</span>
        }
      }
    </ng-template>
  `,
})
export class ThirdProbeCard {
  readonly data = input.required<ThirdProbeDto>();
  /** Null when the viewer may not see the birth date: then there is no clock. */
  readonly dateOfBirth = input<string | null | undefined>(null);

  private readonly now = signal(new Date());

  protected readonly deadline = computed(() => youthEndsOn(this.dateOfBirth(), this.data().limits.youthEndsAtAge));
  protected readonly parts = computed(() => countdownParts(this.deadline() ?? this.now(), this.now()));
  protected readonly days = computed(() => dayWord(this.parts().days));
  protected readonly endLabel = computed(() => this.deadline()?.toLocaleDateString('uk-UA', { day: 'numeric', month: 'long', year: 'numeric' }) ?? '');
  protected readonly checklist = computed(() => thirdProbeChecklist(this.data()));
  protected readonly during = computed(() => this.data().badges.filter((badge) => badge.duringProbe));
  protected readonly earlier = computed(() => this.data().badges.filter((badge) => !badge.duringProbe));
  protected readonly earlierTitles = computed(() => this.earlier().map((badge) => badge.title).join(', '));
  protected readonly resume = computed(() => {
    const blocks = parseProjectMarkdown(this.data().file?.resume ?? '', () => null);
    return hasContent(blocks) ? blocks : [];
  });

  protected readonly projects = computed<ProjectView[]>(() =>
    [...this.data().projects]
      .sort((a, b) => Number(a.isSpare) - Number(b.isSpare))
      .map((project) => ({
        project,
        area: areaOf(project.area),
        facts: [
          ['Ділянка', areaOf(project.area)?.label],
          ['Для кого', reachLabels(project.reach).join(', ')],
          ['Коли', [this.day(project.startsOn), this.day(project.endsOn)].filter(Boolean).join(' — ')],
          ['Де', project.location],
          ['Координатор', [project.coordinatorName, project.coordinatorContact].filter(Boolean).join(' · ')],
        ]
          .filter(([, value]) => !!value)
          .map(([label, value]) => ({ label: label!, value: value! })),
        texts: TEXTS.map((text) => ({
          label: text.label,
          blocks: parseProjectMarkdown(project[text.field], (target) => imageUrl(project, target)),
        })).filter((text) => hasContent(text.blocks)),
      })),
  );

  constructor() {
    const zone = inject(NgZone);
    effect((onCleanup) => {
      if (!this.deadline() || this.parts().isOver) return;
      const timer = zone.runOutsideAngular(() => setInterval(() => this.now.set(new Date()), 1000));
      onCleanup(() => clearInterval(timer));
    });
  }

  protected pad(value: number): string {
    return String(value).padStart(2, '0');
  }

  protected day(value: string | null): string | null {
    if (!value) return null;
    const [year, month, date] = value.split('-').map(Number);
    return shortDate(utcDate(`${year}-${String(month).padStart(2, '0')}-${String(date).padStart(2, '0')}T12:00:00Z`));
  }

  protected marker(block: MarkdownBlock): string {
    if (block.kind !== 'item') return '';
    switch (block.list) {
      case 'ordered':
        return `${block.ordinal}.`;
      case 'checked':
        return '☑';
      case 'unchecked':
        return '☐';
      default:
        return '•';
    }
  }
}

function imageUrl(project: ProbeProjectDto, target: string): string | null {
  if (!target.startsWith(IMAGE_SCHEME)) return null;
  const key = target.slice(IMAGE_SCHEME.length);
  return project.images.find((image) => image.imageKey === key)?.url ?? null;
}
