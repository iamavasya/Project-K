import { Component, input } from '@angular/core';
import { IonBadge } from '@ionic/angular';
import { RoleTag } from './kurin.labels';

/**
 * A row of coloured tags under a name, as the web's tables show offices with `p-tag`. Goes inside
 * an item's `ion-label`, after the lines of text.
 */
@Component({
  selector: 'app-role-tags',
  imports: [IonBadge],
  host: { class: 'role-tags' },
  styles: `
    :host {
      display: flex;
      flex-wrap: wrap;
      gap: 4px;
      margin-top: 6px;
    }
    :host(:empty) {
      display: none;
    }
    ion-badge {
      white-space: normal;
      text-align: start;
    }
  `,
  template: `
    @for (tag of tags(); track $index) {
      <ion-badge [class]="'lk-tag--' + tag.tone">{{ tag.label }}</ion-badge>
    }
  `,
})
export class RoleTags {
  readonly tags = input.required<RoleTag[]>();
}
