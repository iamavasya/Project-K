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
import { kurinAccessGuard } from './features/authModule/guards/kurin.guard';
import { EntityGuard } from './features/authModule/guards/entity.guard';
import { UsersListComponent } from './features/adminModule/components/users-list/users-list';

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
    path: 'users',
    canActivate: [authGuard, roleGuard('Admin'), kurinAccessGuard('panel')],
    component: UsersListComponent,
    data: { breadcrumb: 'Users', parent: '/panel' }
  },
  {
    path: 'panel',
    canActivate: [authGuard, roleGuard('Admin'), kurinAccessGuard('panel')], 
    component: KurinPanelComponent,
    data: { breadcrumb: 'Panel' }
  },
  { 
    path: 'kurin',
    canActivate: [authGuard, kurinAccessGuard('kurin')],
    component: GroupPanelComponent,
    data: { breadcrumb: 'Kurin', parent: '/panel' },
  },
  { 
    path: 'group/:groupKey',
    canActivate: [authGuard, kurinAccessGuard('kurin'), EntityGuard],
    component: MemberPanelComponent,
    data: { breadcrumb: 'Group', parent: '/kurin', entityType: 'group' }
  },
  { 
    path: 'group/:groupKey/member/upsert/:memberKey',
    canActivate: [authGuard, kurinAccessGuard('kurin'), EntityGuard],
    component: UpsertMemberComponent,
    data: { breadcrumb: 'Edit Member', parent: '/group/:groupKey', entityType: 'member' }
  },
  { 
    path: 'group/:groupKey/member/upsert',
    canActivate: [authGuard, kurinAccessGuard('kurin'), EntityGuard],
    component: UpsertMemberComponent,
    data: { breadcrumb: 'New Member', parent: '/group/:groupKey', entityType: 'group' }
  },
  { 
    path: 'member/:memberKey', 
    canActivate: [authGuard, kurinAccessGuard('kurin'), EntityGuard],
    component: MemberCardComponent,
    data: { breadcrumb: 'Member Card', parent: '/group/:groupKey', entityType: 'member' }
  },
  {
    path: 'toolbar',
    component: ToolbarHeader,
  }
];
