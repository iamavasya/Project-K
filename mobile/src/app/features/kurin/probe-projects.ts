import { Component, computed, input } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { IonCard, IonCardContent, IonNote } from '@ionic/angular';
import { shortDate, utcDate } from './kurin.labels';
import { ProbeProjectDto, ProbeProjectsDto } from './kurin.models';
import { MarkdownBlock, hasContent, parseProjectMarkdown } from './project-markdown';

const IMAGE_SCHEME = 'project-image:';

interface ProjectView {
  project: ProbeProjectDto;
  blocks: MarkdownBlock[];
  hasDescription: boolean;
  updated: string;
}

/**
 * The third probe's projects on the phone (web `probe-projects`): read here, written on the web,
 * where the editor and the picture upload live.
 */
@Component({
  selector: 'app-probe-projects',
  imports: [NgTemplateOutlet, IonCard, IonCardContent, IonNote],
  styles: `
    h2 {
      margin: 0;
      font-size: 18px;
      font-weight: 700;
      color: var(--lk-ink);
    }
    h3 {
      margin: 0;
      font-size: 16px;
      font-weight: 700;
      color: var(--lk-ink);
      overflow-wrap: anywhere;
    }
    .head {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 8px;
    }
    .badge {
      border: 1px solid var(--lk-line);
      border-radius: 999px;
      padding: 1px 8px;
      font-size: 11px;
      font-weight: 600;
      color: var(--lk-muted);
    }
    .meta {
      margin: 4px 0 0;
      font-size: 13px;
      color: var(--lk-muted);
    }
    .meta strong {
      color: var(--lk-ink);
    }
    .text {
      margin-top: 8px;
      color: var(--lk-ink);
      font-size: 15px;
      line-height: 1.5;
      overflow-wrap: anywhere;
    }
    .text p {
      margin: 0;
      min-height: 0.75em;
    }
    .text .h {
      font-weight: 700;
      margin-top: 6px;
    }
    .text .li {
      position: relative;
    }
    .text .mark {
      display: inline-block;
      width: 20px;
      margin-left: -20px;
      color: var(--lk-muted);
    }
    .text blockquote {
      margin: 4px 0;
      padding-left: 10px;
      border-left: 1px solid var(--lk-line);
      color: var(--lk-muted);
    }
    .text pre,
    .text code {
      font-family: ui-monospace, Menlo, Consolas, monospace;
      font-size: 0.875em;
      border: 1px solid var(--lk-line);
      border-radius: 8px;
    }
    .text code {
      padding: 0 4px;
    }
    .text pre {
      padding: 8px 10px;
      white-space: pre-wrap;
    }
    .text a {
      color: var(--lk-primary);
    }
    .text img {
      display: block;
      max-width: 100%;
      max-height: 320px;
      margin: 8px 0;
      border: 1px solid var(--lk-line);
      border-radius: 8px;
    }
    .b { font-weight: 600; }
    .i { font-style: italic; }
    .s { text-decoration: line-through; }
  `,
  template: `
    <ion-card data-testid="probe-projects">
      <ion-card-content>
        <h2>Проєкти</h2>
        <p class="meta">{{ projects().length }} з {{ data().limits.maxProjects }} · один із них індивідуальний</p>
        @if (!projects().length) {
          <p class="meta">Проєктів ще не описано.</p>
        }
        @if (data().canEdit) {
          <p class="meta">Описати чи змінити проєкт можна у вебверсії Лілейки: там є редактор і фото.</p>
        }
      </ion-card-content>
    </ion-card>

    @for (item of projects(); track item.project.projectKey) {
      <ion-card data-testid="probe-project">
        <ion-card-content>
          <div class="head">
            <h3>{{ item.project.title }}</h3>
            @if (item.project.isIndividual) { <span class="badge">Індивідуальний</span> }
          </div>
          @if (item.project.directionMentorName || item.project.directionMentorContact) {
            <p class="meta">
              Ментор з напряму: <strong>{{ item.project.directionMentorName || 'без імені' }}</strong>
              @if (item.project.directionMentorContact) { · {{ item.project.directionMentorContact }} }
            </p>
          }
          @if (item.hasDescription) {
            <div class="text">
              @for (block of item.blocks; track $index) {
                @switch (block.kind) {
                  @case ('heading') {
                    <p class="h"><ng-container *ngTemplateOutlet="runsTpl; context: { $implicit: block.runs }" /></p>
                  }
                  @case ('item') {
                    <p class="li" [style.padding-left.px]="20 + block.indent * 20">
                      <span class="mark" aria-hidden="true">{{ marker(block) }}</span>
                      <ng-container *ngTemplateOutlet="runsTpl; context: { $implicit: block.runs }" />
                    </p>
                  }
                  @case ('quote') {
                    <blockquote><ng-container *ngTemplateOutlet="runsTpl; context: { $implicit: block.runs }" /></blockquote>
                  }
                  @case ('code') { <pre>{{ block.text }}</pre> }
                  @default {
                    <p><ng-container *ngTemplateOutlet="runsTpl; context: { $implicit: block.runs }" /></p>
                  }
                }
              }
            </div>
          } @else {
            <p class="meta">Опису ще немає.</p>
          }
          <ion-note class="meta">{{ item.updated }}</ion-note>
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
export class ProbeProjectsCard {
  readonly data = input.required<ProbeProjectsDto>();

  protected readonly projects = computed<ProjectView[]>(() =>
    this.data().projects.map((project) => {
      const blocks = parseProjectMarkdown(project.description, (target) => imageUrl(project, target));
      return {
        project,
        blocks,
        hasDescription: hasContent(blocks),
        updated: [`Оновлено ${shortDate(utcDate(project.updatedAtUtc))}`, project.updatedByName].filter(Boolean).join(' · '),
      };
    }),
  );

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
