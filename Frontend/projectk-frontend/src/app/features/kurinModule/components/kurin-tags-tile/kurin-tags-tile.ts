import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { ConfirmationService } from '@openng/optimus-ui/api';
import { ConfirmDialogModule } from '@openng/optimus-ui/confirmdialog';
import { MemberService } from '../../services/member-service/member.service';
import { KurinTagService } from '../../services/kurin-tag-service/kurin-tag.service';
import { KurinTagManagerComponent, TagCandidate } from '../kurin-tag-manager/kurin-tag-manager';
import { MemberDto } from '../../models/member.dto';

/**
 * «Теги куреня» на сторінці куреня — для того, кому курінь дав право ними керувати, навіть коли
 * реєстр йому закритий (курінний старшого куреня). Склад, кому ставити теги, читає сам.
 */
@Component({
  selector: 'app-kurin-tags-tile',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [KurinTagManagerComponent, ConfirmDialogModule],
  providers: [ConfirmationService],
  template: `
    @if (tagService.canManage()) {
      <p-confirmdialog />
      <section class="kurin-tile" aria-labelledby="kurin-tags-tile-title">
        <div class="lil-tile-head">
          <div>
            <h2 id="kurin-tags-tile-title">Теги куреня</h2>
            <span class="lil-count">{{ tagService.current().tags.length }}</span>
          </div>
        </div>
        <app-kurin-tag-manager [kurinKey]="kurinKey()" [candidates]="candidates()" />
      </section>
    }
  `
})
export class KurinTagsTileComponent {
  protected readonly tagService = inject(KurinTagService);
  private readonly members = inject(MemberService);

  readonly kurinKey = input.required<string>();

  private readonly people = signal<MemberDto[]>([]);
  protected readonly candidates = computed<TagCandidate[]>(() => this.people().map(member => ({
    memberKey: member.memberKey,
    name: [member.lastName, member.firstName].filter(Boolean).join(' '),
    tagKeys: member.tagKeys ?? []
  })).sort((a, b) => a.name.localeCompare(b.name, 'uk')));

  constructor() {
    effect(() => {
      const kurinKey = this.kurinKey();
      this.tagService.revision();
      if (!kurinKey) {
        return;
      }
      untracked(() => {
        this.tagService.load(kurinKey).subscribe({ error: () => undefined });
        this.members.getAll(undefined, kurinKey).subscribe({ next: people => this.people.set(people), error: () => undefined });
      });
    });
  }
}
