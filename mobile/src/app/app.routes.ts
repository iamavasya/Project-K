import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'tabs',
    loadComponent: () => import('./pages/tabs').then((m) => m.TabsPage),
    children: [
      { path: 'home', loadComponent: () => import('./pages/home').then((m) => m.HomePage) },
      { path: 'more', loadComponent: () => import('./pages/more').then((m) => m.MorePage) },
      { path: '', redirectTo: 'home', pathMatch: 'full' },
    ],
  },
  { path: '', redirectTo: 'tabs/home', pathMatch: 'full' },
];
