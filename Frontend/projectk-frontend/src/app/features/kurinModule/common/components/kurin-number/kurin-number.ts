import { Component, ChangeDetectionStrategy, input } from '@angular/core';


@Component({
  selector: 'app-kurin-number',
  templateUrl: './kurin-number.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './kurin-number.css'
})
export class KurinNumberComponent {
  readonly number = input<number | null>(null);
}
