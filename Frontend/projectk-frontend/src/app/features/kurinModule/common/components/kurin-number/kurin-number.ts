import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-kurin-number',
  imports: [CommonModule],
  templateUrl: './kurin-number.html',
  styleUrl: './kurin-number.css'
})
export class KurinNumberComponent {
  @Input() number: number | null = null;
}
