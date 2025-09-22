import { Component, EventEmitter, Input, Output } from '@angular/core';
import { DrawerModule } from 'primeng/drawer';
import { ButtonModule } from 'primeng/button';
import { PanelMenuModule } from 'primeng/panelmenu';
import { MenuItem } from 'primeng/api';

@Component({
  selector: 'app-sidebar-menu',
  imports: [DrawerModule, ButtonModule, PanelMenuModule],
  templateUrl: './sidebar-menu.html',
  styleUrl: './sidebar-menu.scss'
})
export class SidebarMenu {
  @Input() visible: boolean = false;
  @Output() visibleChange: EventEmitter<boolean> = new EventEmitter<boolean>();

  // TODO: Продовжити роботу з стейтом, а саме:
  // 1. Якщо адмін входить в курінь з панелі - додавати в стейт курінь кей, прибирати з історії панель, і показувати кнопку назад в панель, яка прибиратиме курінь кей з стейту
  // 2. Наповнити меню динамічною інформацією з бекенду і додати роутінг
  
  items: MenuItem[] = [
    {
      label: 'Kurin',
    },
    {
      label: 'Groups'
    },
    {
      label: 'All Members'
    },
    {
      label: 'Settings'
    }
  ];

  close() {
    this.visible = false;
    this.visibleChange.emit(this.visible);
  }
}
