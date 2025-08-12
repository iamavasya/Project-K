import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { DialogModule } from 'primeng/dialog';
import { CommonModule } from '@angular/common';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { FormsModule } from '@angular/forms';
import { KurinDto } from '../../models/kurinDto';

@Component({
  selector: 'app-manage-panel',
  imports: [DialogModule, CommonModule, InputTextModule, ButtonModule, FormsModule],
  templateUrl: './manage-panel.html',
  styleUrl: './manage-panel.scss'
})
export class ManagePanel implements OnChanges {
  @Input() visible = false;
  @Input() parameter: 'create' | 'update' | 'delete' | 'undef'= 'undef';
  @Input() kurin: KurinDto | null = null;
  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() actionPerformed = new EventEmitter<{action: 'create' | 'update' | 'delete', kurin: KurinDto}>();

  kurinKey = this.kurin?.kurinKey;
  number = this.kurin?.number;

  ngOnChanges(changes: SimpleChanges): void {
    if(changes['kurin'] && this.kurin) {
      this.kurinKey = this.kurin.kurinKey;
      this.number = this.kurin.number;
    }
  }

  hide(): void {
    this.visible = false;
    this.visibleChange.emit(this.visible);
  }

  onActionClick(action: 'create' | 'update' | 'delete'): void {
    const updatedKurin: KurinDto = {
      ...(this.kurin || {}),
      kurinKey: this.kurinKey,
      number: this.number
    } as KurinDto;

    this.actionPerformed.emit({ action, kurin: updatedKurin });

    this.hide();
  }
}
