import { Routes } from '@angular/router';
import { signedInGuard, signedOutGuard } from './auth/auth.guards';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [signedOutGuard],
    loadComponent: () => import('./pages/login').then((m) => m.LoginPage),
  },
  {
    path: 'tabs',
    canActivate: [signedInGuard],
    loadComponent: () => import('./pages/tabs').then((m) => m.TabsPage),
    children: [
      { path: 'home', loadComponent: () => import('./pages/home').then((m) => m.HomePage) },
      { path: 'more', loadComponent: () => import('./pages/more').then((m) => m.MorePage) },
      { path: '', redirectTo: 'home', pathMatch: 'full' },
    ],
  },
  { path: '', redirectTo: 'tabs/home', pathMatch: 'full' },
  { path: '**', redirectTo: 'tabs/home' },
];
