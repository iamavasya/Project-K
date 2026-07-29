import { AsyncPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { BreadcrumbModule } from 'primeng/breadcrumb';
import { BreadcrumbService } from '../../services/breadcrumb-service/breadcrumb-service';


@Component({
  selector: 'app-breadcrumb',
  standalone: true,
  imports: [BreadcrumbModule, AsyncPipe],
  template: `
    @if (home$ | async; as home) {
      <p-breadcrumb [model]="(breadcrumbs$ | async) ?? []" [home]="home" [homeAriaLabel]="home.title ?? ''" />
    }
  `
})
export class BreadcrumbComponent {
  private readonly breadcrumbService = inject(BreadcrumbService);
  protected readonly breadcrumbs$ = this.breadcrumbService.breadcrumbs$;
  protected readonly home$ = this.breadcrumbService.home$;
}
