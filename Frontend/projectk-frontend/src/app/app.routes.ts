import { Routes } from '@angular/router';
import { KurinPanelComponent } from './features/kurinModule/kurin-panel/kurin-panel.component';
import { GroupPanelComponent } from './features/kurinModule/group-panel/group-panel.component';
import { MemberPanelComponent } from './features/kurinModule/member-panel/member-panel.component';
import { MemberCardComponent } from './features/kurinModule/member-card/member-card.component';
import { UpsertMemberComponent } from './features/kurinModule/upsert-member/upsert-member.component';
import { authGuard } from './features/authModule/guards/auth.guard';
import { roleGuard } from './features/authModule/guards/role.guard';
import { LoginComponent } from './features/authModule/login-component/login-component';
import { LogoutComponent } from './features/authModule/logout-component/logout-component';
import { ToolbarHeader } from './features/kurinModule/common/components/toolbar-header/toolbar-header';

export const routes: Routes = [
  {
    path: 'login',
    component: LoginComponent,
    data: { breadcrumb: 'Login' }
  },
  {
    path: 'logout',
    canActivate: [authGuard],
    component: LogoutComponent,
    data: { breadcrumb: 'Logout' }
  },
  {
    path: 'panel',
    canActivate: [authGuard, roleGuard('Admin')], 
    component: KurinPanelComponent,
    data: { breadcrumb: 'Panel' }
  },
  { 
    path: 'kurin/:kurinKey', 
    component: GroupPanelComponent,
    data: { breadcrumb: 'Kurin', parent: '/panel' }
  },
  { 
    path: 'group/:groupKey', 
    component: MemberPanelComponent,
    data: { breadcrumb: 'Group', parent: '/kurin/:kurinKey' }
  },
  { 
    path: 'group/:groupKey/member/upsert/:memberKey', 
    component: UpsertMemberComponent,
    data: { breadcrumb: 'Edit Member', parent: '/group/:groupKey' }
  },
  { 
    path: 'group/:groupKey/member/upsert', 
    component: UpsertMemberComponent,
    data: { breadcrumb: 'New Member', parent: '/group/:groupKey' }
  },
  { 
    path: 'member/:memberKey', 
    component: MemberCardComponent,
    data: { breadcrumb: 'Member Card', parent: '/group/:groupKey' }
  },
  {
    path: 'toolbar',
    component: ToolbarHeader,
  }
];
