import { Routes } from '@angular/router';
import { mfaSetupGuard, signedInGuard, signedOutGuard } from './auth/auth.guards';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [signedOutGuard],
    loadComponent: () => import('./pages/login').then((m) => m.LoginPage),
  },
  {
    path: 'mfa',
    canActivate: [signedInGuard],
    loadComponent: () => import('./pages/mfa-setup').then((m) => m.MfaSetupPage),
  },
  {
    path: 'tabs',
    canActivate: [signedInGuard, mfaSetupGuard],
    loadComponent: () => import('./pages/tabs').then((m) => m.TabsPage),
    // Each tab keeps its own stack; screens a tab opens live under it, so «back» stays in the tab.
    children: [
      {
        path: 'home',
        children: [
          { path: '', loadComponent: () => import('./pages/home').then((m) => m.HomePage) },
          {
            path: 'notifications',
            loadComponent: () => import('./features/leader/notifications.page').then((m) => m.NotificationsPage),
          },
        ],
      },
      {
        path: 'calendar',
        children: [
          { path: '', loadComponent: () => import('./features/calendar/calendar.page').then((m) => m.CalendarPage) },
          { path: 'new', loadComponent: () => import('./features/calendar/event-form.page').then((m) => m.EventFormPage) },
          {
            path: 'edit/:itemKey',
            loadComponent: () => import('./features/calendar/event-form.page').then((m) => m.EventFormPage),
          },
          { path: 'event/:itemKey', loadComponent: () => import('./features/calendar/event.page').then((m) => m.EventPage) },
          {
            path: 'attendance/:itemKey',
            loadComponent: () => import('./features/score/attendance.page').then((m) => m.AttendancePage),
          },
        ],
      },
      {
        path: 'tasks',
        children: [
          { path: '', loadComponent: () => import('./features/tasks/tasks.page').then((m) => m.TasksPage) },
          { path: 'new', loadComponent: () => import('./features/tasks/task-form.page').then((m) => m.TaskFormPage) },
          { path: 'edit/:itemKey', loadComponent: () => import('./features/tasks/task-form.page').then((m) => m.TaskFormPage) },
          { path: 'task/:itemKey', loadComponent: () => import('./features/tasks/task.page').then((m) => m.TaskPage) },
        ],
      },
      {
        path: 'kurin',
        children: [
          { path: '', loadComponent: () => import('./features/kurin/kurin.page').then((m) => m.KurinPage) },
          { path: 'group/:groupKey', loadComponent: () => import('./features/kurin/group.page').then((m) => m.GroupPage) },
          {
            path: 'group/:groupKey/score',
            loadComponent: () => import('./features/score/group-score.page').then((m) => m.GroupScorePage),
          },
          {
            path: 'group/:groupKey/dues',
            loadComponent: () => import('./features/leader/group-dues.page').then((m) => m.GroupDuesPage),
          },
          { path: 'member/:memberKey', loadComponent: () => import('./features/kurin/member.page').then((m) => m.MemberPage) },
          {
            path: 'member/:memberKey/probe',
            loadComponent: () => import('./features/kurin/probe.page').then((m) => m.ProbePage),
          },
          { path: 'score', loadComponent: () => import('./features/score/kurin-score.page').then((m) => m.KurinScorePage) },
          {
            path: 'review/skills',
            loadComponent: () => import('./features/leader/skills-review.page').then((m) => m.SkillsReviewPage),
          },
        ],
      },
      {
        path: 'more',
        children: [
          { path: '', loadComponent: () => import('./pages/more').then((m) => m.MorePage) },
          { path: 'profile', loadComponent: () => import('./pages/profile').then((m) => m.ProfilePage) },
          { path: 'about', loadComponent: () => import('./pages/about').then((m) => m.AboutPage) },
          { path: 'account', loadComponent: () => import('./features/account/account.page').then((m) => m.AccountPage) },
          {
            path: 'appearance',
            loadComponent: () => import('./features/account/appearance.page').then((m) => m.AppearancePage),
          },
          { path: 'kurins', loadComponent: () => import('./features/account/kurins.page').then((m) => m.KurinsPage) },
          { path: 'privacy', loadComponent: () => import('./features/account/privacy.page').then((m) => m.PrivacyPage) },
          { path: 'report', loadComponent: () => import('./features/account/report.page').then((m) => m.ReportPage) },
        ],
      },
      { path: '', redirectTo: 'home', pathMatch: 'full' },
    ],
  },
  // The component sheet (Ionic as it ships next to Лілейка). Open without signing in.
  { path: 'ui', loadComponent: () => import('./pages/ui-sheet').then((m) => m.UiSheetPage) },
  { path: '', redirectTo: 'tabs/home', pathMatch: 'full' },
  { path: '**', redirectTo: 'tabs/home' },
];
