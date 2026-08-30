import { Routes } from '@angular/router';
import { authGuard } from './features/authModule/guards/auth.guard';
import { publicAuthRedirectGuard } from './features/authModule/guards/public-auth-redirect.guard';
import { setupGuard } from './features/authModule/guards/setup.guard';
import { capabilityGuard } from './features/authModule/guards/capability.guard';
import { kurinAccessGuard } from './features/authModule/guards/kurin.guard';
import { EntityGuard } from './features/authModule/guards/entity.guard';
import { leadershipAccessGuard } from './features/authModule/guards/leadership-access.guard';

export const routes: Routes = [
  {
    path: '',
    canActivate: [publicAuthRedirectGuard],
    loadComponent: () => import('./features/systemModule/components/welcome-page/welcome-page')
      .then(m => m.WelcomePageComponent),
    data: { breadcrumb: 'Вітання' }
  },
  {
    path: 'setup',
    canActivate: [setupGuard],
    loadComponent: () => import('./features/authModule/setup/setup')
      .then(m => m.SetupComponent),
    title: 'Початкове налаштування',
    data: { breadcrumb: 'Налаштування' }
  },
  {
    path: 'welcome',
    canActivate: [publicAuthRedirectGuard],
    loadComponent: () => import('./features/systemModule/components/welcome-page/welcome-page')
      .then(m => m.WelcomePageComponent),
    data: { breadcrumb: 'Вітання' }
  },
  {
    path: 'join',
    canActivate: [publicAuthRedirectGuard],
    loadComponent: () => import('./features/authModule/onboarding/waitlist-registration/waitlist-registration')
      .then(m => m.WaitlistRegistrationComponent),
    title: 'Приєднатися',
    data: { breadcrumb: 'Заявка' }
  },
  {
    path: 'activate/:token',
    canActivate: [publicAuthRedirectGuard],
    loadComponent: () => import('./features/authModule/onboarding/account-activation/account-activation')
      .then(m => m.AccountActivationComponent),
    title: 'Активація акаунта',
    data: { breadcrumb: 'Активація' }
  },
  {
    path: 'login',
    canActivate: [publicAuthRedirectGuard],
    loadComponent: () => import('./features/authModule/login/login')
      .then(m => m.LoginComponent),
    title: 'Вхід',
    data: { breadcrumb: 'Вхід' }
  },
  {
    path: 'logout',
    canActivate: [authGuard],
    loadComponent: () => import('./features/authModule/logout/logout')
      .then(m => m.LogoutComponent),
    title: 'Вихід',
    data: { breadcrumb: 'Вихід' }
  },
  {
    path: 'settings/account',
    canActivate: [authGuard],
    loadComponent: () => import('./features/authModule/account-settings/account-settings')
      .then(m => m.AccountSettingsComponent),
    title: 'Налаштування акаунта',
    data: { breadcrumb: 'Акаунт' }
  },
  {
    path: 'forbidden',
    loadComponent: () => import('./features/authModule/forbidden/forbidden')
      .then(m => m.ForbiddenComponent),
    title: 'Немає доступу',
    data: { breadcrumb: 'Немає доступу' }
  },
  {
    path: 'users',
    canActivate: [authGuard, kurinAccessGuard('panel'), capabilityGuard('admin')],
    loadComponent: () => import('./features/adminModule/components/users-list/users-list')
      .then(m => m.UsersListComponent),
    title: 'Користувачі',
    data: { breadcrumb: 'Користувачі', parent: '/panel' }
  },
  {
    path: 'waitlist',
    canActivate: [authGuard, kurinAccessGuard('panel'), capabilityGuard('admin')],
    loadComponent: () => import('./features/adminModule/components/waitlist-management/waitlist-management')
      .then(m => m.WaitlistManagementComponent),
    title: 'Заявки',
    data: { breadcrumb: 'Заявки', parent: '/panel' }
  },
  {
    path: 'announcements',
    canActivate: [authGuard, kurinAccessGuard('panel'), capabilityGuard('admin')],
    loadComponent: () => import('./features/adminModule/components/public-announcements/public-announcements')
      .then(m => m.PublicAnnouncementsComponent),
    title: 'Оголошення',
    data: { breadcrumb: 'Оголошення', parent: '/panel' }
  },
  {
    path: 'system-settings',
    canActivate: [authGuard, kurinAccessGuard('panel'), capabilityGuard('admin')],
    loadComponent: () => import('./features/adminModule/components/system-settings/system-settings')
      .then(m => m.SystemSettingsComponent),
    title: 'Системні налаштування',
    data: { breadcrumb: 'Системні налаштування', parent: '/panel' }
  },
  {
    path: 'panel',
    canActivate: [authGuard, kurinAccessGuard('panel'), capabilityGuard('admin')], 
    loadComponent: () => import('./features/kurinModule/admin-panel/admin-panel')
      .then(m => m.AdminPanelComponent),
    title: 'Адміністрація',
    data: { breadcrumb: 'Адміністрація' }
  },
  { 
    path: 'kurin',
    canActivate: [authGuard, kurinAccessGuard('kurin')],
    loadComponent: () => import('./features/kurinModule/kurin-panel/kurin-panel')
      .then(m => m.KurinPanelComponent),
    title: 'Курінь',
    data: { breadcrumb: 'Курінь', parent: '/panel', parentRoles: ['Admin'], titleContext: 'kurin', breadcrumbEntity: 'kurin' },
  },
  { 
    path: 'group/:groupKey',
    canActivate: [authGuard, kurinAccessGuard('kurin'), EntityGuard],
    loadComponent: () => import('./features/kurinModule/group-panel/group-panel')
      .then(m => m.GroupPanelComponent),
    title: 'Гурток',
    data: { breadcrumb: 'Гурток', parent: '/kurin', entityType: 'group', titleContext: 'group', breadcrumbEntity: 'group' }
  },
  { 
    path: 'group/:groupKey/member/upsert/:memberKey',
    canActivate: [authGuard, kurinAccessGuard('kurin'), EntityGuard],
    loadComponent: () => import('./features/kurinModule/upsert-member/upsert-member')
      .then(m => m.UpsertMemberComponent),
    title: 'Редагування учасника',
    data: { breadcrumb: 'Редагування учасника', parent: '/group/:groupKey', entityType: 'member', entityAction: 'Update', titleContext: 'member' }
  },
  { 
    path: 'group/:groupKey/member/upsert',
    canActivate: [authGuard, kurinAccessGuard('kurin'), EntityGuard],
    loadComponent: () => import('./features/kurinModule/upsert-member/upsert-member')
      .then(m => m.UpsertMemberComponent),
    title: 'Новий учасник',
    data: { breadcrumb: 'Новий учасник', parent: '/group/:groupKey', entityType: 'group', entityAction: 'Create', titleContext: 'group' }
  },
  {
    // Editing a member who belongs to no group: the group-scoped twin above cannot be
    // used, its :groupKey would be Guid.Empty.
    path: 'kurin/:kurinKey/member/upsert/:memberKey',
    canActivate: [authGuard, kurinAccessGuard('kurin'), EntityGuard],
    loadComponent: () => import('./features/kurinModule/upsert-member/upsert-member')
      .then(m => m.UpsertMemberComponent),
    title: 'Редагування учасника',
    data: {
      breadcrumb: 'Редагування учасника',
      parent: '/member/:memberKey',
      entityType: 'member',
      entityAction: 'Update',
      titleContext: 'member'
    }
  },
  {
    path: 'kurin/:kurinKey/member/upsert',
    canActivate: [authGuard, kurinAccessGuard('kurin'), EntityGuard],
    loadComponent: () => import('./features/kurinModule/upsert-member/upsert-member')
      .then(m => m.UpsertMemberComponent),
    title: 'Новий учасник',
    data: { breadcrumb: 'Новий учасник', parent: '/kurin', entityType: 'kurin', entityAction: 'Create', titleContext: 'kurin' }
  },
  { 
    path: 'member/:memberKey/probe/:probeId',
    canActivate: [authGuard, kurinAccessGuard('kurin'), EntityGuard],
    loadComponent: () => import('./features/kurinModule/member-probe-page/member-probe-page')
      .then(m => m.MemberProbePageComponent),
    title: 'Проба',
    data: { breadcrumb: 'Проба', parent: '/member/:memberKey', entityType: 'member', titleContext: 'member' }
  },
  {
    path: 'member/:memberKey', 
    canActivate: [authGuard, kurinAccessGuard('kurin'), EntityGuard],
    loadComponent: () => import('./features/kurinModule/member-card/member-card')
      .then(m => m.MemberCardComponent),
    title: 'Картка учасника',
    data: {
      breadcrumb: 'Картка учасника',
      parent: '/group/:groupKey',
      parentFallback: '/kurin',
      entityType: 'member',
      titleContext: 'member',
      breadcrumbEntity: 'member'
    }
  },
  {
    path: 'leadership/create/:type/:entityKey',
    canActivate: [authGuard, kurinAccessGuard('kurin'), leadershipAccessGuard, EntityGuard],
    loadComponent: () => import('./features/kurinModule/common/components/leadership/leadership/leadership')
      .then(m => m.LeadershipComponent),
    title: 'Новий провід',
    data: {
      breadcrumb: 'Новий провід',
      parent: '/kurin',
      entityTypeParam: 'type',
      entityKeyParam: 'entityKey',
      entityAction: 'Create'
    }
  },
  {
    path: 'leadership/:leadershipKey/:type/:entityKey',
    canActivate: [authGuard, kurinAccessGuard('kurin'), leadershipAccessGuard, EntityGuard],
    loadComponent: () => import('./features/kurinModule/common/components/leadership/leadership/leadership')
      .then(m => m.LeadershipComponent),
    title: 'Провід',
    data: {
      breadcrumb: 'Провід',
      parent: '/kurin',
      entityTypeParam: 'type',
      entityKeyParam: 'entityKey'
    }
  },
  {
    path: 'kurin/:kurinKey/review/skills',
    canActivate: [authGuard, kurinAccessGuard('kurin'), EntityGuard],
    loadComponent: () => import('./features/kurinModule/skills-review-page/skills-review-page')
      .then(m => m.SkillsReviewPageComponent),
    title: 'Перевірка вмінь',
    data: { breadcrumb: 'Модерація вмілостей', parent: '/kurin', entityType: 'kurin', titleContext: 'kurin' }
  },
  {
    path: 'kurin/:kurinKey/settings',
    canActivate: [authGuard, kurinAccessGuard('kurin'), capabilityGuard('admin', 'kurinManagement'), EntityGuard],
    loadComponent: () => import('./features/kurinModule/kurin-settings/kurin-settings')
      .then(m => m.KurinSettingsComponent),
    title: 'Налаштування куреня',
    data: { breadcrumb: 'Налаштування куреня', parent: '/kurin', entityType: 'kurin', entityAction: 'Update', titleContext: 'kurin' }
  },
  {
    path: 'planning/create/:kurinKey',
    canActivate: [authGuard, kurinAccessGuard('planning-create')],
    loadComponent: () => import('./features/kurinModule/create-planning/create-planning')
      .then(m => m.CreatePlanningComponent),
    title: 'Нове планування',
    data: { breadcrumb: 'Нове планування', parent: '/kurin', entityType: 'kurin', titleContext: 'kurin' }
  },
  {
    path: 'planning/:kurinKey',
    canActivate: [authGuard, kurinAccessGuard('planning'), EntityGuard],
    loadComponent: () => import('./features/kurinModule/planning-list/planning-list')
      .then(m => m.PlanningListComponent),
    title: 'Планування',
    data: { breadcrumb: 'Планування', parent: '/kurin', entityType: 'kurin', titleContext: 'kurin' }
  },
  {
    path: 'calendar/:kurinKey',
    canActivate: [authGuard, kurinAccessGuard('kurin'), EntityGuard],
    loadComponent: () => import('./features/kurinModule/agenda-calendar/agenda-calendar')
      .then(m => m.AgendaCalendarComponent),
    title: 'Календар',
    data: { breadcrumb: 'Календар', parent: '/kurin', entityType: 'kurin', titleContext: 'kurin' }
  },
  {
    path: 'tasks/:kurinKey',
    canActivate: [authGuard, kurinAccessGuard('kurin'), EntityGuard],
    loadComponent: () => import('./features/kurinModule/agenda-board/agenda-board')
      .then(m => m.AgendaBoardComponent),
    title: 'Задачі',
    data: { breadcrumb: 'Задачі', parent: '/kurin', entityType: 'kurin', titleContext: 'kurin' }
  }
];
