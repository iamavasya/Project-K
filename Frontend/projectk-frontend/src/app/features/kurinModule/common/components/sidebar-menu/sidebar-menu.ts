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
