import { Routes } from '@angular/router';
import { authGuard } from './features/authModule/guards/auth.guard';
import { publicAuthRedirectGuard } from './features/authModule/guards/public-auth-redirect.guard';
import { roleGuard } from './features/authModule/guards/role.guard';
import { kurinAccessGuard } from './features/authModule/guards/kurin.guard';
import { EntityGuard } from './features/authModule/guards/entity.guard';
import { leadershipAccessGuard } from './features/authModule/guards/leadership-access.guard';

export const routes: Routes = [
  {
    path: '',
    canActivate: [publicAuthRedirectGuard],
    loadComponent: () => import('./features/systemModule/components/welcome-page/welcome-page')
      .then(m => m.WelcomePageComponent),
    data: { breadcrumb: 'Welcome' }
  },
  {
    path: 'setup',
    loadComponent: () => import('./features/authModule/setup-component/setup.component')
      .then(m => m.SetupComponent),
    data: { breadcrumb: 'Setup' }
  },
  {
    path: 'welcome',
    canActivate: [publicAuthRedirectGuard],
    loadComponent: () => import('./features/systemModule/components/welcome-page/welcome-page')
      .then(m => m.WelcomePageComponent),
    data: { breadcrumb: 'Welcome' }
  },
  {
    path: 'join',
    canActivate: [publicAuthRedirectGuard],
    loadComponent: () => import('./features/authModule/onboarding/waitlist-registration/waitlist-registration')
      .then(m => m.WaitlistRegistrationComponent),
    data: { breadcrumb: 'Join Waitlist' }
  },
  {
    path: 'activate/:token',
    canActivate: [publicAuthRedirectGuard],
    loadComponent: () => import('./features/authModule/onboarding/account-activation/account-activation')
      .then(m => m.AccountActivationComponent),
    data: { breadcrumb: 'Activate Account' }
  },
  {
    path: 'login',
    canActivate: [publicAuthRedirectGuard],
    loadComponent: () => import('./features/authModule/login-component/login-component')
      .then(m => m.LoginComponent),
    data: { breadcrumb: 'Login' }
  },
  {
    path: 'logout',
    canActivate: [authGuard],
    loadComponent: () => import('./features/authModule/logout-component/logout-component')
      .then(m => m.LogoutComponent),
    data: { breadcrumb: 'Logout' }
  },
  {
    path: 'settings/account',
    canActivate: [authGuard],
    loadComponent: () => import('./features/authModule/account-settings/account-settings.component')
      .then(m => m.AccountSettingsComponent),
    data: { breadcrumb: 'Account Settings' }
  },
  {
    path: 'forbidden',
    loadComponent: () => import('./features/authModule/forbidden-component/forbidden-component')
      .then(m => m.ForbiddenComponent),
    data: { breadcrumb: 'Forbidden' }
  },
  {
    path: 'users',
    canActivate: [authGuard, kurinAccessGuard('panel'), roleGuard('Admin')],
    loadComponent: () => import('./features/adminModule/components/users-list/users-list')
      .then(m => m.UsersListComponent),
    data: { breadcrumb: 'Users', parent: '/panel' }
  },
  {
    path: 'waitlist',
    canActivate: [authGuard, kurinAccessGuard('panel'), roleGuard('Admin')],
    loadComponent: () => import('./features/adminModule/components/waitlist-management/waitlist-management')
      .then(m => m.WaitlistManagementComponent),
    data: { breadcrumb: 'Waitlist', parent: '/panel' }
  },
  {
    path: 'announcements',
    canActivate: [authGuard, kurinAccessGuard('panel'), roleGuard('Admin')],
    loadComponent: () => import('./features/adminModule/components/public-announcements/public-announcements')
      .then(m => m.PublicAnnouncementsComponent),
    data: { breadcrumb: 'Announcements', parent: '/panel' }
  },
  {
    path: 'system-settings',
    canActivate: [authGuard, kurinAccessGuard('panel'), roleGuard('Admin')],
    loadComponent: () => import('./features/adminModule/components/system-settings/system-settings.component')
      .then(m => m.SystemSettingsComponent),
    data: { breadcrumb: 'System Settings', parent: '/panel' }
  },
  {
    path: 'panel',
    canActivate: [authGuard, kurinAccessGuard('panel'), roleGuard('Admin')], 
    loadComponent: () => import('./features/kurinModule/admin-panel/admin-panel.component')
      .then(m => m.AdminPanelComponent),
    data: { breadcrumb: 'Panel' }
  },
  { 
    path: 'kurin',
    canActivate: [authGuard, kurinAccessGuard('kurin')],
    loadComponent: () => import('./features/kurinModule/kurin-panel/kurin-panel.component')
      .then(m => m.KurinPanelComponent),
    data: { breadcrumb: 'Kurin', parent: '/panel', parentRoles: ['Admin'] },
  },
  { 
    path: 'group/:groupKey',
    canActivate: [authGuard, kurinAccessGuard('kurin'), EntityGuard],
    loadComponent: () => import('./features/kurinModule/group-panel/group-panel.component')
      .then(m => m.GroupPanelComponent),
    data: { breadcrumb: 'Group', parent: '/kurin', entityType: 'group' }
  },
  { 
    path: 'group/:groupKey/member/upsert/:memberKey',
    canActivate: [authGuard, kurinAccessGuard('kurin'), EntityGuard],
    loadComponent: () => import('./features/kurinModule/upsert-member/upsert-member.component')
      .then(m => m.UpsertMemberComponent),
    data: { breadcrumb: 'Edit Member', parent: '/group/:groupKey', entityType: 'member', entityAction: 'Update' }
  },
  { 
    path: 'group/:groupKey/member/upsert',
    canActivate: [authGuard, kurinAccessGuard('kurin'), EntityGuard],
    loadComponent: () => import('./features/kurinModule/upsert-member/upsert-member.component')
      .then(m => m.UpsertMemberComponent),
    data: { breadcrumb: 'New Member', parent: '/group/:groupKey', entityType: 'group', entityAction: 'Create' }
  },
  {
    path: 'kurin/:kurinKey/member/upsert',
    canActivate: [authGuard, kurinAccessGuard('kurin'), EntityGuard],
    loadComponent: () => import('./features/kurinModule/upsert-member/upsert-member.component')
      .then(m => m.UpsertMemberComponent),
    data: { breadcrumb: 'New Kurin Member', parent: '/kurin', entityType: 'kurin', entityAction: 'Create' }
  },
  { 
    path: 'member/:memberKey/probe/:probeId',
    canActivate: [authGuard, kurinAccessGuard('kurin'), EntityGuard],
    loadComponent: () => import('./features/kurinModule/member-probe-page/member-probe-page.component')
      .then(m => m.MemberProbePageComponent),
    data: { breadcrumb: 'Probe Details', parent: '/member/:memberKey', entityType: 'member' }
  },
  {
    path: 'member/:memberKey', 
    canActivate: [authGuard, kurinAccessGuard('kurin'), EntityGuard],
    loadComponent: () => import('./features/kurinModule/member-card/member-card.component')
      .then(m => m.MemberCardComponent),
    data: {
      breadcrumb: 'Member Card',
      parent: '/group/:groupKey',
      parentFallback: '/kurin',
      entityType: 'member'
    }
  },
  {
    path: 'toolbar',
    canActivate: [authGuard],
    loadComponent: () => import('./features/kurinModule/common/components/toolbar-header/toolbar-header')
      .then(m => m.ToolbarHeader),
    data: { breadcrumb: 'Toolbar' }
  },
  {
    path: 'leadership/create/:type/:entityKey',
    canActivate: [authGuard, kurinAccessGuard('kurin'), leadershipAccessGuard, EntityGuard],
    loadComponent: () => import('./features/kurinModule/common/components/leadership/leadership-component/leadership-component')
      .then(m => m.LeadershipComponent),
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
    loadComponent: () => import('./features/kurinModule/common/components/leadership/leadership-component/leadership-component')
      .then(m => m.LeadershipComponent),
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
    loadComponent: () => import('./features/kurinModule/skills-review-page/skills-review-page.component')
      .then(m => m.SkillsReviewPageComponent),
    data: { breadcrumb: 'Skills Review', parent: '/kurin', entityType: 'kurin' }
  },
  {
    path: 'kurin/:kurinKey/settings',
    canActivate: [authGuard, kurinAccessGuard('kurin'), roleGuard('Admin', 'Manager'), EntityGuard],
    loadComponent: () => import('./features/kurinModule/kurin-settings/kurin-settings.component')
      .then(m => m.KurinSettingsComponent),
    data: { breadcrumb: 'Kurin Settings', parent: '/kurin', entityType: 'kurin', entityAction: 'Update' }
  },
  {
    path: 'planning/create/:kurinKey',
    canActivate: [authGuard, kurinAccessGuard('planning-create')],
    loadComponent: () => import('./features/kurinModule/create-planning/create-planning')
      .then(m => m.CreatePlanningComponent),
    data: { breadcrumb: 'New Planning', parent: '/kurin', entityType: 'kurin' }
  },
  {
    path: 'planning/:kurinKey',
    canActivate: [authGuard, kurinAccessGuard('planning'), EntityGuard],
    loadComponent: () => import('./features/kurinModule/planning-list/planning-list')
      .then(m => m.PlanningListComponent),
    data: { breadcrumb: 'Planning', parent: '/kurin', entityType: 'kurin' }
  }
];
