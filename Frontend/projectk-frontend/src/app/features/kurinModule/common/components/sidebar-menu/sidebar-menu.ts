import { Component, EventEmitter, inject, Input, Output } from '@angular/core';
import { DrawerModule } from 'primeng/drawer';
import { ButtonModule } from 'primeng/button';
import { PanelMenuModule } from 'primeng/panelmenu';
import { MenuItem } from 'primeng/api';
import { BaseIcon } from "primeng/icons/baseicon";
import { Router } from '@angular/router';

@Component({
  selector: 'app-sidebar-menu',
  imports: [DrawerModule, ButtonModule, PanelMenuModule],
  templateUrl: './sidebar-menu.html',
  styleUrl: './sidebar-menu.scss'
})
export class SidebarMenu {
  private readonly router = inject(Router);
  @Input() visible: boolean = false;
  @Input() kurinKey: string | null = null;
  @Output() visibleChange: EventEmitter<boolean> = new EventEmitter<boolean>();

  // TODO: Продовжити роботу з стейтом, а саме:
  // 1. Якщо адмін входить в курінь з панелі - додавати в стейт курінь кей, прибирати з історії панель, і показувати кнопку назад в панель, яка прибиратиме курінь кей з стейту
  // 2. Наповнити меню динамічною інформацією з бекенду і додати роутінг
  
  items: MenuItem[] = [
    {
      label: 'Kurin',
      routerLink: ['/kurin'],
      command: () => {
        this.close();
        this.router.navigate(['/kurin']);
      }
    },
    // NOT IMPLEMENTED YET
    {
      label: 'Groups',
      disabled: true
    },
    {
      label: 'All Members',
      disabled: true
    },
    {
      label: 'Settings',
      disabled: true
    }
  ];

  close() {
    this.visible = false;
    this.visibleChange.emit(this.visible);
  }
}
