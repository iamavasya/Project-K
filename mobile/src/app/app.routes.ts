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
    children: [
      { path: 'home', loadComponent: () => import('./pages/home').then((m) => m.HomePage) },
      {
        path: 'more',
        children: [
          { path: '', loadComponent: () => import('./pages/more').then((m) => m.MorePage) },
          { path: 'profile', loadComponent: () => import('./pages/profile').then((m) => m.ProfilePage) },
          { path: 'about', loadComponent: () => import('./pages/about').then((m) => m.AboutPage) },
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
