import { WaitlistAttentionService } from '../../../adminModule/services/waitlist-attention/waitlist-attention.service';
import { Component, inject, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { TableModule } from '@openng/optimus-ui/table';
import { SplitButtonModule } from '@openng/optimus-ui/splitbutton';

import { KurinDto } from '../../models/kurin.dto';
import { KraiDto } from '../../models/krai.dto';
import { of, switchMap } from 'rxjs';
import { KurinService } from '../../services/kurin-service/kurin.service';
import { MenuItem, MessageService } from '@openng/optimus-ui/api';
import { ManageAction, ManagePanelComponent, ManagePanelConfig, ManagePanelField } from '../../components/manage-panel/manage-panel';
import { ButtonModule } from '@openng/optimus-ui/button';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../authModule/services/auth-service/auth.service';
import { EmptyStateComponent } from '../../../../shared/empty-state/empty-state';
import { failureDetail } from '../../../../shared/functions/failure-detail.function';

@Component({
  selector: 'app-admin-panel',
  imports: [TableModule, SplitButtonModule, ManagePanelComponent, ButtonModule, EmptyStateComponent, RouterModule],
  templateUrl: './admin-panel.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './admin-panel.css'
})
export class AdminPanelComponent implements OnInit {

  private readonly router: Router = inject(Router);
  private readonly kurinService = inject(KurinService);
  private readonly authService = inject(AuthService);
  private readonly messageService = inject(MessageService);
  readonly waitlistAttention = inject(WaitlistAttentionService);

  selectedItem: KurinDto | null = null;
  managePanelVisible = false;
  managePanelParameter: 'create' | 'update' | 'delete' | 'undef' = 'undef';
  data: KurinDto[] = [];
  krais: KraiDto[] = [];

  actions: MenuItem[] = [];

  managePanelConfig: ManagePanelConfig = {
    entityType: 'kurin',
    title: 'Курінь',
    fields: [
      {
        name: 'kurinKey',
        label: 'Системний ключ',
        type: 'text',
        required: true,
        hiddenOn: ['create', 'delete'],
        disabledOn: ['update']
      },
      {
        name: 'number',
        label: 'Номер куреня',
        type: 'number',
        placeholder: 'Наприклад: 101',
        required: true,
        hiddenOn: ['delete'],
      },
      AdminPanelComponent.kraiField([])
    ],
    displayName: (entity: KurinDto) => {
      const name = entity.number === null ? 'Підготовчий курінь' : `${entity.number} курінь`;
      return entity.namedAfter ? `${name} ім. ${entity.namedAfter}` : name;
    },
    createFactory: () => ({ kurinKey: '', number: null, kraiCode: null }),
  }

  private static kraiField(krais: KraiDto[]): ManagePanelField {
    return {
      name: 'kraiCode',
      label: 'Край',
      type: 'select',
      placeholder: 'Типовий край інстансу',
      hiddenOn: ['delete'],
      options: krais.map(krai => ({ label: krai.name, value: krai.code }))
    };
  }

  prepareItemActions(item: KurinDto): void {
    this.actions = [
      {
        label: 'Редагувати',
        icon: 'pi pi-pencil',
        command: () => { this.onActionClick(item, 'update') }
      },
      {
        label: 'Видалити',
        icon: 'pi pi-trash',
        command: () => { this.onActionClick(item, 'delete') }
      }
    ];
  }

  ngOnInit(): void {
    this.refreshData();
    this.kurinService.getKrais().subscribe((krais: KraiDto[]) => {
      this.krais = krais ?? [];
      this.data = this.sortedByKrai(this.data);
      // A new config object, so the panel rebuilds its form with the choices in it.
      this.managePanelConfig = {
        ...this.managePanelConfig,
        fields: this.managePanelConfig.fields.map(field =>
          field.name === 'kraiCode' ? AdminPanelComponent.kraiField(this.krais) : field)
      };
    });
    this.waitlistAttention.refresh();
  }

  onActionClick(item: KurinDto | null, param: ManageAction | 'undef'): void {
    this.selectedItem = item;
    this.managePanelVisible = true;
    this.managePanelParameter = param;
  }

  onManageAction(e: { action: ManageAction; entity: KurinDto; entityType: string }): void {
    switch (e.action) {
      case 'create':
        // The kurin alone. Its Звʼязковий arrives through the waitlist as a kurin-leader candidate,
        // or is seated by an admin once the person has an account — the old "email of the
        // Звʼязковий" field posted to a route the API never had.
        this.kurinService.createKurin(e.entity).subscribe(() => { this.refreshData(); });
        break;
      case 'update': {
        // The край moves through its own admin-only call, after the rest of the record is saved.
        const previousKrai = this.selectedItem?.kraiCode;
        const nextKrai = e.entity.kraiCode;
        this.kurinService.updateKurin(e.entity).pipe(
          switchMap(() => nextKrai && nextKrai !== previousKrai
            ? this.kurinService.setKurinKrai(e.entity.kurinKey, nextKrai)
            : of(null))
        ).subscribe({
          next: () => this.refreshData(),
          error: (error: unknown) => {
            this.refreshData();
            this.messageService.add({
              severity: 'error',
              summary: 'Не вдалося зберегти курінь',
              detail: failureDetail(error)
            });
          }
        });
        break;
      }
      case 'delete':
        this.kurinService.deleteKurin(e.entity.kurinKey).subscribe(() => this.refreshData());
        break;
    }
  }

  refreshData(): void {
    this.kurinService.getKurins().subscribe((data: KurinDto[]) => {
      this.data = this.sortedByKrai(data ?? []);
    });
  }

  /** The kurins grouped by край: Україна first, then the rest by name; by number inside a край. */
  private sortedByKrai(kurins: KurinDto[]): KurinDto[] {
    const rank = (code?: string) => code === 'ua' ? '' : this.kraiName(code);
    return [...kurins].sort((a, b) =>
      rank(a.kraiCode).localeCompare(rank(b.kraiCode), 'uk')
      || (a.number ?? Number.MAX_SAFE_INTEGER) - (b.number ?? Number.MAX_SAFE_INTEGER));
  }

  kraiName(code?: string): string {
    return this.krais.find(krai => krai.code === code)?.name ?? code ?? '';
  }

  kurinCountInKrai(code?: string): number {
    return this.data.filter(kurin => kurin.kraiCode === code).length;
  }

  onOpenClick(kurinKey: string): void {
    this.authService.setKurinScope(kurinKey).subscribe({
      next: () => this.router.navigate(['/kurin']),
      error: (error: unknown) => this.messageService.add({
        severity: 'error',
        summary: 'Не вдалося відкрити курінь',
        detail: failureDetail(error)
      })
    });
  }
}
