import { Routes } from '@angular/router';
import { authGuard } from '../guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: '/login', pathMatch: 'full' },
  {
    path: 'login',
    loadComponent: () =>
      import('../pages/auth/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'register',
    loadComponent: () =>
      import('../pages/auth/register/register.component').then(m => m.RegisterComponent)
  },
  {
    path: 'admin/dashboard',
    loadComponent: () =>
      import('../pages/admin/dashboard/admin-dashboard.component').then(m => m.AdminDashboardComponent),
    canActivate: [authGuard],
    data: { role: 'Admin' }
  },
  {
    path: 'admin/alerts',
    loadComponent: () =>
      import('../pages/admin/fraud-alerts/fraud-alerts.component').then(m => m.FraudAlertsComponent),
    canActivate: [authGuard],
    data: { role: 'Admin' }
  },
  {
    path: 'user/dashboard',
    loadComponent: () =>
      import('../pages/user/user-dashboard.component').then(m => m.UserDashboardComponent),
    canActivate: [authGuard]
  },
  { path: '**', redirectTo: '/login' }
];

