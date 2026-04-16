import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TagModule } from 'primeng/tag';

@Component({
  selector: 'app-skill-mini-card',
  imports: [TagModule],
  templateUrl: './skill-mini-card.component.html',
  styleUrl: './skill-mini-card.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SkillMiniCardComponent {
  readonly title = input.required<string>();
  readonly imageUrl = input<string | null>(null);
  readonly isPendingConfirmation = input(false);
}
