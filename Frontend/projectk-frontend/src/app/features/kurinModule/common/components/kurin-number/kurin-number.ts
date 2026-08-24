import { Component, Input, ChangeDetectionStrategy } from '@angular/core';


@Component({
  selector: 'app-kurin-number',
  templateUrl: './kurin-number.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './kurin-number.css'
})
export class KurinNumberComponent {
  @Input() number: number | null = null;
}
