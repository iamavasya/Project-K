import { Routes } from '@angular/router';
import { AdminPanelComponent } from './features/kurinModule/admin-panel/admin-panel.component';
import { KurinPanelComponent } from './features/kurinModule/kurin-panel/kurin-panel.component';
import { GroupPanelComponent } from './features/kurinModule/group-panel/group-panel.component';
import { MemberCardComponent } from './features/kurinModule/member-card/member-card.component';
import { UpsertMemberComponent } from './features/kurinModule/upsert-member/upsert-member.component';
import { authGuard } from './features/authModule/guards/auth.guard';
import { publicAuthRedirectGuard } from './features/authModule/guards/public-auth-redirect.guard';
import { roleGuard } from './features/authModule/guards/role.guard';
import { LoginComponent } from './features/authModule/login-component/login-component';
import { LogoutComponent } from './features/authModule/logout-component/logout-component';
import { ForbiddenComponent } from './features/authModule/forbidden-component/forbidden-component';
import { ToolbarHeader } from './features/kurinModule/common/components/toolbar-header/toolbar-header';
import { kurinAccessGuard } from './features/authModule/guards/kurin.guard';
import { EntityGuard } from './features/authModule/guards/entity.guard';
import { UsersListComponent } from './features/adminModule/components/users-list/users-list';
import { LeadershipComponent } from './features/kurinModule/common/components/leadership/leadership-component/leadership-component';
import { PlanningListComponent } from './features/kurinModule/planning-list/planning-list';
import { CreatePlanningComponent } from './features/kurinModule/create-planning/create-planning';
import { MemberProbePageComponent } from './features/kurinModule/member-probe-page/member-probe-page.component';
import { SkillsReviewPageComponent } from './features/kurinModule/skills-review-page/skills-review-page.component';
import { leadershipAccessGuard } from './features/authModule/guards/leadership-access.guard';

import { WaitlistRegistrationComponent } from './features/authModule/onboarding/waitlist-registration/waitlist-registration';
import { AccountActivationComponent } from './features/authModule/onboarding/account-activation/account-activation';
import { WaitlistManagementComponent } from './features/adminModule/components/waitlist-management/waitlist-management';
import { AccountSettingsComponent } from './features/authModule/account-settings/account-settings.component';
import { PublicAnnouncementsComponent } from './features/adminModule/components/public-announcements/public-announcements';
import { WelcomePageComponent } from './features/systemModule/components/welcome-page/welcome-page';

export const routes: Routes = [
  {
    path: '',
    canActivate: [publicAuthRedirectGuard],
    component: WelcomePageComponent,
    data: { breadcrumb: 'Welcome' }
  },
  {
    path: 'welcome',
    canActivate: [publicAuthRedirectGuard],
    component: WelcomePageComponent,
    data: { breadcrumb: 'Welcome' }
  },
  {
    path: 'join',
    canActivate: [publicAuthRedirectGuard],
    component: WaitlistRegistrationComponent,
    data: { breadcrumb: 'Join Waitlist' }
  },
  {
    path: 'activate/:token',
    canActivate: [publicAuthRedirectGuard],
    component: AccountActivationComponent,
    data: { breadcrumb: 'Activate Account' }
  },
  {
    path: 'login',
    canActivate: [publicAuthRedirectGuard],
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
    path: 'settings/account',
    canActivate: [authGuard],
    component: AccountSettingsComponent,
    data: { breadcrumb: 'Account Settings' }
  },
  {
    path: 'forbidden',
    component: ForbiddenComponent,
    data: { breadcrumb: 'Forbidden' }
  },
  {
    path: 'users',
    canActivate: [authGuard, kurinAccessGuard('panel'), roleGuard('Admin')],
    component: UsersListComponent,
    data: { breadcrumb: 'Users', parent: '/panel' }
  },
  {
    path: 'waitlist',
    canActivate: [authGuard, kurinAccessGuard('panel'), roleGuard('Admin')],
    component: WaitlistManagementComponent,
    data: { breadcrumb: 'Waitlist', parent: '/panel' }
  },
  {
    path: 'announcements',
    canActivate: [authGuard, kurinAccessGuard('panel'), roleGuard('Admin')],
    component: PublicAnnouncementsComponent,
    data: { breadcrumb: 'Announcements', parent: '/panel' }
  },
  {
    path: 'panel',
    canActivate: [authGuard, kurinAccessGuard('panel'), roleGuard('Admin')], 
    component: AdminPanelComponent,
    data: { breadcrumb: 'Panel' }
  },
  { 
    path: 'kurin',
    canActivate: [authGuard, kurinAccessGuard('kurin')],
    component: KurinPanelComponent,
    data: { breadcrumb: 'Kurin', parent: '/panel' },
  },
  { 
    path: 'group/:groupKey',
    canActivate: [authGuard, kurinAccessGuard('kurin'), EntityGuard],
    component: GroupPanelComponent,
    data: { breadcrumb: 'Group', parent: '/kurin', entityType: 'group' }
  },
  { 
    path: 'group/:groupKey/member/upsert/:memberKey',
    canActivate: [authGuard, kurinAccessGuard('kurin'), EntityGuard],
    component: UpsertMemberComponent,
    data: { breadcrumb: 'Edit Member', parent: '/group/:groupKey', entityType: 'member', entityAction: 'Update' }
  },
  { 
    path: 'group/:groupKey/member/upsert',
    canActivate: [authGuard, kurinAccessGuard('kurin'), EntityGuard],
    component: UpsertMemberComponent,
    data: { breadcrumb: 'New Member', parent: '/group/:groupKey', entityType: 'group', entityAction: 'Create' }
  },
  {
    path: 'kurin/:kurinKey/member/upsert',
    canActivate: [authGuard, kurinAccessGuard('kurin'), EntityGuard],
    component: UpsertMemberComponent,
    data: { breadcrumb: 'New Kurin Member', parent: '/kurin', entityType: 'kurin', entityAction: 'Create' }
  },
  { 
    path: 'member/:memberKey/probe/:probeId',
    canActivate: [authGuard, kurinAccessGuard('kurin'), EntityGuard],
    component: MemberProbePageComponent,
    data: { breadcrumb: 'Probe Details', parent: '/member/:memberKey', entityType: 'member' }
  },
  {
    path: 'member/:memberKey', 
    canActivate: [authGuard, kurinAccessGuard('kurin'), EntityGuard],
    component: MemberCardComponent,
    data: { breadcrumb: 'Member Card', parent: '/group/:groupKey', entityType: 'member' }
  },
  {
    path: 'toolbar',
    canActivate: [authGuard],
    component: ToolbarHeader,
    data: { breadcrumb: 'Toolbar' }
  },
  {
    path: 'leadership/create/:type/:entityKey',
    canActivate: [authGuard, kurinAccessGuard('kurin'), leadershipAccessGuard, EntityGuard],
    component: LeadershipComponent,
    data: {
      breadcrumb: 'Create Leadership',
      parent: '/kurin',
      entityTypeParam: 'type',
      entityKeyParam: 'entityKey',
      entityAction: 'Create'
    }
  },
  {
    path: 'leadership/:leadershipKey/:type/:entityKey',
    canActivate: [authGuard, kurinAccessGuard('kurin'), leadershipAccessGuard, EntityGuard],
    component: LeadershipComponent,
    data: {
      breadcrumb: 'Edit Leadership',
      parent: '/kurin',
      entityTypeParam: 'type',
      entityKeyParam: 'entityKey'
    }
  },
  {
    path: 'kurin/:kurinKey/review/skills',
    canActivate: [authGuard, kurinAccessGuard('kurin'), EntityGuard],
    component: SkillsReviewPageComponent,
    data: { breadcrumb: 'Skills Review', parent: '/kurin', entityType: 'kurin' }
  },
  {
    path: 'planning/create/:kurinKey',
    canActivate: [authGuard, kurinAccessGuard('planning-create')],
    component: CreatePlanningComponent,
    data: { breadcrumb: 'New Planning', parent: '/kurin', entityType: 'kurin' }
  },
  {
    path: 'planning/:kurinKey',
    canActivate: [authGuard, kurinAccessGuard('planning'), EntityGuard],
    component: PlanningListComponent,
    data: { breadcrumb: 'Planning', parent: '/kurin', entityType: 'kurin' }
  }
];
