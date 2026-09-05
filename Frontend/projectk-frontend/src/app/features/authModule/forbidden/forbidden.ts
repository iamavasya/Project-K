import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonModule } from '@openng/optimus-ui/button';

@Component({
  selector: 'app-forbidden-component',
  imports: [RouterLink, ButtonModule],
  templateUrl: './forbidden.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './forbidden.css'
})
export class ForbiddenComponent {}
