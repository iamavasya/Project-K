import { ChangeDetectionStrategy, Component, inject, input, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from '@openng/optimus-ui/button';
import { InputTextModule } from '@openng/optimus-ui/inputtext';
import { ToggleSwitchModule } from '@openng/optimus-ui/toggleswitch';
import { MessageService } from '@openng/optimus-ui/api';
import { AgendaService } from '../../services/agenda-service/agenda.service';

/**
 * How long a kurin's done tasks stay on the board, and how long its archive is kept. Deleting for
 * good is the kurin's call, so each step can be switched off; saved on its own, like the event groups.
 */
@Component({
  selector: 'app-agenda-archive-policy',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, ButtonModule, InputTextModule, ToggleSwitchModule],
  templateUrl: './agenda-archive-policy.html',
  styleUrl: './agenda-archive-policy.css'
})
export class AgendaArchivePolicyComponent implements OnInit {
  private readonly agenda = inject(AgendaService);
  private readonly messages = inject(MessageService);

  readonly kurinKey = input.required<string>();

  protected readonly loaded = signal(false);
  protected readonly saving = signal(false);
  protected autoArchive = true;
  protected autoArchiveDays = 30;
  protected purge = true;
  protected purgeDays = 365;

  ngOnInit(): void {
    this.agenda.getArchivePolicy(this.kurinKey()).subscribe({
      next: policy => {
        this.autoArchive = policy.autoArchiveAfterDays !== null;
        this.autoArchiveDays = policy.autoArchiveAfterDays ?? 30;
        this.purge = policy.purgeAfterDays !== null;
        this.purgeDays = policy.purgeAfterDays ?? 365;
        this.loaded.set(true);
      },
      error: () => this.loaded.set(true)
    });
  }

  protected valid(): boolean {
    const inRange = (days: number) => Number.isInteger(days) && days >= 1 && days <= 3650;
    return (!this.autoArchive || inRange(this.autoArchiveDays)) && (!this.purge || inRange(this.purgeDays));
  }

  save(): void {
    if (!this.valid()) {
      this.messages.add({ severity: 'warn', summary: 'Строк — від 1 до 3650 днів' });
      return;
    }
    this.saving.set(true);
    this.agenda.setArchivePolicy(this.kurinKey(), {
      autoArchiveAfterDays: this.autoArchive ? this.autoArchiveDays : null,
      purgeAfterDays: this.purge ? this.purgeDays : null
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.messages.add({ severity: 'success', summary: 'Збережено' });
      },
      error: () => {
        this.saving.set(false);
        this.messages.add({ severity: 'error', summary: 'Не вдалося зберегти' });
      }
    });
  }
}
