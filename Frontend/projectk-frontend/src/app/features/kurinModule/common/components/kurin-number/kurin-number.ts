import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-kurin-number',
  imports: [CommonModule],
  templateUrl: './kurin-number.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './kurin-number.css'
})
export class KurinNumberComponent {
  @Input() number: number | null = null;
}
