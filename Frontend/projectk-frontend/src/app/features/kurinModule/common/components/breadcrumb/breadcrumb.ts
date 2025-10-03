import { Component, inject, OnInit } from '@angular/core';
import { MenuItem } from 'primeng/api';
import { BreadcrumbModule } from 'primeng/breadcrumb';
import { BreadcrumbService } from '../../services/breadcrumb-service/breadcrumb-service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-breadcrumb',
  standalone: true,
  imports: [BreadcrumbModule, CommonModule],
  template: `
    <p-breadcrumb [model]="items" [home]="home"></p-breadcrumb>
  `
})
export class BreadcrumbComponent implements OnInit {
  items: MenuItem[] = [];
  home: MenuItem = { icon: 'pi pi-home', routerLink: '/' };
  private readonly breadcrumbService = inject(BreadcrumbService);

  ngOnInit() {
    this.breadcrumbService.breadcrumbs$.subscribe(breadcrumbs => {
      this.items = breadcrumbs;
    });
  }
}
