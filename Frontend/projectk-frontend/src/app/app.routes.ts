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
    loadComponent: () => import('./features/systemModule/pages/welcome-page/welcome-page')
      .then(m => m.WelcomePageComponent),
    data: { breadcrumb: 'Вітання' }
  },
  {
    path: 'setup',
    canActivate: [setupGuard],
    loadComponent: () => import('./features/authModule/pages/setup/setup')
      .then(m => m.SetupComponent),
    title: 'Початкове налаштування',
    data: { breadcrumb: 'Налаштування' }
  },
  {
    path: 'welcome',
    canActivate: [publicAuthRedirectGuard],
    loadComponent: () => import('./features/systemModule/pages/welcome-page/welcome-page')
      .then(m => m.WelcomePageComponent),
    data: { breadcrumb: 'Вітання' }
  },
  {
    path: 'about',
    loadComponent: () => import('./features/systemModule/pages/about-page/about-page')
      .then(m => m.AboutPageComponent),
    title: 'Про Лілейку',
    data: { breadcrumb: 'Про Лілейку' }
  },
  {
    path: 'forgot-password',
    loadComponent: () => import('./features/authModule/pages/onboarding/forgot-password/forgot-password')
      .then(m => m.ForgotPasswordComponent),
    title: 'Відновлення пароля'
  },
  {
    // Where the link in the reset email lands; it carries token and email as query parameters.
    path: 'reset-password',
    loadComponent: () => import('./features/authModule/pages/onboarding/reset-password/reset-password')
      .then(m => m.ResetPasswordComponent),
    title: 'Новий пароль'
  },
  {
    path: 'join',
    canActivate: [publicAuthRedirectGuard],
    loadComponent: () => import('./features/authModule/pages/onboarding/waitlist-registration/waitlist-registration')
      .then(m => m.WaitlistRegistrationComponent),
    title: 'Приєднатися',
    data: { breadcrumb: 'Заявка' }
  },
  {
    path: 'activate/:token',
    canActivate: [publicAuthRedirectGuard],
    loadComponent: () => import('./features/authModule/pages/onboarding/account-activation/account-activation')
      .then(m => m.AccountActivationComponent),
    title: 'Активація акаунта',
    data: { breadcrumb: 'Активація' }
  },
  {
    path: 'login',
    canActivate: [publicAuthRedirectGuard],
    loadComponent: () => import('./features/authModule/pages/login/login')
      .then(m => m.LoginComponent),
    title: 'Вхід',
    data: { breadcrumb: 'Вхід' }
  },
  {
    path: 'logout',
    canActivate: [authGuard],
    loadComponent: () => import('./features/authModule/pages/logout/logout')
      .then(m => m.LogoutComponent),
    title: 'Вихід',
    data: { breadcrumb: 'Вихід' }
  },
  {
    path: 'settings/account',
    canActivate: [authGuard],
    loadComponent: () => import('./features/authModule/pages/account-settings/account-settings')
      .then(m => m.AccountSettingsComponent),
    title: 'Налаштування акаунта',
    data: { breadcrumb: 'Акаунт' }
  },
  {
    path: 'forbidden',
    loadComponent: () => import('./features/authModule/pages/forbidden/forbidden')
      .then(m => m.ForbiddenComponent),
    title: 'Немає доступу',
    data: { breadcrumb: 'Немає доступу' }
  },
  {
    path: 'users',
    canActivate: [authGuard, kurinAccessGuard('panel'), capabilityGuard('admin')],
    loadComponent: () => import('./features/adminModule/pages/users-list/users-list')
      .then(m => m.UsersListComponent),
    title: 'Користувачі',
    data: { breadcrumb: 'Користувачі', parent: '/panel' }
  },
  {
    path: 'waitlist',
    canActivate: [authGuard, kurinAccessGuard('panel'), capabilityGuard('admin')],
    loadComponent: () => import('./features/adminModule/pages/waitlist-management/waitlist-management')
      .then(m => m.WaitlistManagementComponent),
    title: 'Заявки',
    data: { breadcrumb: 'Заявки', parent: '/panel' }
  },
  {
    path: 'system-settings',
    canActivate: [authGuard, kurinAccessGuard('panel'), capabilityGuard('admin')],
    loadComponent: () => import('./features/adminModule/pages/system-settings/system-settings')
      .then(m => m.SystemSettingsComponent),
    title: 'Системні налаштування',
    data: { breadcrumb: 'Системні налаштування', parent: '/panel' }
  },
  {
    path: 'panel',
    canActivate: [authGuard, kurinAccessGuard('panel'), capabilityGuard('admin')], 
    loadComponent: () => import('./features/kurinModule/pages/admin-panel/admin-panel')
      .then(m => m.AdminPanelComponent),
    title: 'Адміністрація',
    data: { breadcrumb: 'Адміністрація' }
  },
  { 
    path: 'kurin',
    canActivate: [authGuard, kurinAccessGuard('kurin')],
    loadComponent: () => import('./features/kurinModule/pages/kurin-panel/kurin-panel')
      .then(m => m.KurinPanelComponent),
    title: 'Курінь',
    data: { breadcrumb: 'Курінь', parent: '/panel', parentRoles: ['Admin'], titleContext: 'kurin', breadcrumbEntity: 'kurin' },
  },
  {
    path: 'kurin/import',
    canActivate: [authGuard, kurinAccessGuard('kurin'), capabilityGuard('admin', 'kurinManagement')],
    loadComponent: () => import('./features/kurinModule/pages/import/import')
      .then(m => m.RosterImportComponent),
    title: 'Імпорт складу',
    data: { breadcrumb: 'Імпорт складу', parent: '/kurin', titleContext: 'kurin' }
  },
  {
    path: 'kurin/registry',
    canActivate: [authGuard, kurinAccessGuard('kurin'), capabilityGuard('admin', 'kurinManagement', 'groupLeadership')],
    loadComponent: () => import('./features/kurinModule/pages/registry/registry')
      .then(m => m.RegistryComponent),
    title: 'Реєстр',
    data: { breadcrumb: 'Реєстр', parent: '/kurin', titleContext: 'kurin' }
  },
  { 
    path: 'group/:groupKey',
    canActivate: [authGuard, kurinAccessGuard('kurin'), EntityGuard],
    loadComponent: () => import('./features/kurinModule/pages/group-panel/group-panel')
      .then(m => m.GroupPanelComponent),
    title: 'Гурток',
    data: { breadcrumb: 'Гурток', parent: '/kurin', entityType: 'group', titleContext: 'group', breadcrumbEntity: 'group' }
  },
  { 
    path: 'group/:groupKey/member/upsert/:memberKey',
    canActivate: [authGuard, kurinAccessGuard('kurin'), EntityGuard],
    loadComponent: () => import('./features/kurinModule/pages/upsert-member/upsert-member')
      .then(m => m.UpsertMemberComponent),
    title: 'Редагування учасника',
    data: { breadcrumb: 'Редагування учасника', parent: '/group/:groupKey', entityType: 'member', entityAction: 'Update', titleContext: 'member' }
  },
  { 
    path: 'group/:groupKey/member/upsert',
    canActivate: [authGuard, kurinAccessGuard('kurin'), EntityGuard],
    loadComponent: () => import('./features/kurinModule/pages/upsert-member/upsert-member')
      .then(m => m.UpsertMemberComponent),
    title: 'Новий учасник',
    data: { breadcrumb: 'Новий учасник', parent: '/group/:groupKey', entityType: 'group', entityAction: 'Create', titleContext: 'group' }
  },
  {
    // Editing a member who belongs to no group: the group-scoped twin above cannot be
    // used, its :groupKey would be Guid.Empty.
    path: 'kurin/:kurinKey/member/upsert/:memberKey',
    canActivate: [authGuard, kurinAccessGuard('kurin'), EntityGuard],
    loadComponent: () => import('./features/kurinModule/pages/upsert-member/upsert-member')
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
    loadComponent: () => import('./features/kurinModule/pages/upsert-member/upsert-member')
      .then(m => m.UpsertMemberComponent),
    title: 'Новий учасник',
    data: { breadcrumb: 'Новий учасник', parent: '/kurin', entityType: 'kurin', entityAction: 'Create', titleContext: 'kurin' }
  },
  { 
    path: 'member/:memberKey/probe/:probeId',
    canActivate: [authGuard, kurinAccessGuard('kurin'), EntityGuard],
    loadComponent: () => import('./features/kurinModule/pages/member-probe-page/member-probe-page')
      .then(m => m.MemberProbePageComponent),
    title: 'Проба',
    data: { breadcrumb: 'Проба', parent: '/member/:memberKey', entityType: 'member', titleContext: 'member' }
  },
  {
    path: 'member/:memberKey', 
    canActivate: [authGuard, kurinAccessGuard('kurin'), EntityGuard],
    loadComponent: () => import('./features/kurinModule/pages/member-card/member-card')
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
    loadComponent: () => import('./features/kurinModule/components/leadership/leadership/leadership')
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
    loadComponent: () => import('./features/kurinModule/components/leadership/leadership/leadership')
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
    loadComponent: () => import('./features/kurinModule/pages/skills-review-page/skills-review-page')
      .then(m => m.SkillsReviewPageComponent),
    title: 'Перевірка вмінь',
    data: { breadcrumb: 'Модерація вмілостей', parent: '/kurin', entityType: 'kurin', titleContext: 'kurin' }
  },
  {
    path: 'kurin/:kurinKey/settings',
    canActivate: [authGuard, kurinAccessGuard('kurin'), capabilityGuard('admin', 'kurinManagement'), EntityGuard],
    loadComponent: () => import('./features/kurinModule/pages/kurin-settings/kurin-settings')
      .then(m => m.KurinSettingsComponent),
    title: 'Налаштування куреня',
    data: { breadcrumb: 'Налаштування куреня', parent: '/kurin', entityType: 'kurin', entityAction: 'Update', titleContext: 'kurin' }
  },
  {
    path: 'planning/create/:kurinKey',
    canActivate: [authGuard, kurinAccessGuard('planning-create')],
    loadComponent: () => import('./features/kurinModule/pages/create-planning/create-planning')
      .then(m => m.CreatePlanningComponent),
    title: 'Нове планування',
    data: { breadcrumb: 'Нове планування', parent: '/kurin', entityType: 'kurin', titleContext: 'kurin' }
  },
  {
    path: 'planning/:kurinKey',
    canActivate: [authGuard, kurinAccessGuard('planning'), EntityGuard],
    loadComponent: () => import('./features/kurinModule/pages/planning-list/planning-list')
      .then(m => m.PlanningListComponent),
    title: 'Планування',
    data: { breadcrumb: 'Планування', parent: '/kurin', entityType: 'kurin', titleContext: 'kurin' }
  },
  {
    path: 'calendar/:kurinKey',
    canActivate: [authGuard, kurinAccessGuard('kurin'), EntityGuard],
    loadComponent: () => import('./features/kurinModule/pages/agenda-calendar/agenda-calendar')
      .then(m => m.AgendaCalendarComponent),
    title: 'Календар',
    data: { breadcrumb: 'Календар', parent: '/kurin', entityType: 'kurin', titleContext: 'kurin' }
  },
  {
    path: 'tasks/:kurinKey',
    canActivate: [authGuard, kurinAccessGuard('kurin'), EntityGuard],
    loadComponent: () => import('./features/kurinModule/pages/agenda-board/agenda-board')
      .then(m => m.AgendaBoardComponent),
    title: 'Задачі',
    data: { breadcrumb: 'Задачі', parent: '/kurin', entityType: 'kurin', titleContext: 'kurin' }
  }
];
