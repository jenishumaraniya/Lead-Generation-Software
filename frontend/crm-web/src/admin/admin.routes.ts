import { Routes } from '@angular/router';
import { authGuard } from '../app/core/guards/auth.guard';
import { roleGuard } from '../app/core/guards/role.guard';

export const ADMIN_ROUTES: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./auth/login.component')
        .then(m => m.LoginComponent)
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./admin-layout/admin-layout.component')
        .then(m => m.AdminLayoutComponent),
    children: [
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full'
      },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./dashboard/dashboard.component')
            .then(m => m.DashboardComponent)
      },
      {
        path: 'products',
        loadComponent: () =>
          import('./products/product-management.component')
            .then(m => m.ProductManagementComponent)
      },
      {
        path: 'categories',
        loadComponent: () =>
          import('./categories/category-management.component')
            .then(m => m.CategoryManagementComponent)
      },
      {
        path: 'prospects',
        loadComponent: () =>
          import('./prospects/prospect-hub.component')
            .then(m => m.ProspectHubComponent)
      },
      {
        path: 'campaigns',
        loadComponent: () =>
          import('./campaigns/campaign-hub.component')
            .then(m => m.CampaignHubComponent)
      },
      {
        path: 'leads',
        loadComponent: () =>
          import('./leads/lead-pipeline.component')
            .then(m => m.LeadPipelineComponent)
      },
      {
        path: 'handoff',
        loadComponent: () =>
          import('./handoff/handoff-hub.component')
            .then(m => m.HandoffHubComponent)
      },
      {
        path: 'visitors',
        loadComponent: () =>
          import('./visitor/visitor-list/visitor-list.component')
            .then(m => m.VisitorListComponent)
      },
      // Admin Only Routes
      {
        path: 'scoring',
        canActivate: [roleGuard],
        data: { role: 'ADMIN' },
        loadComponent: () =>
          import('./scoring/scoring-hub.component')
            .then(m => m.ScoringHubComponent)
      },
      {
        path: 'users',
        canActivate: [roleGuard],
        data: { role: 'ADMIN' },
        loadComponent: () =>
          import('./users/user-management.component')
            .then(m => m.UserManagementComponent)
      },
      {
        path: 'audit-logs',
        canActivate: [roleGuard],
        data: { role: 'ADMIN' },
        loadComponent: () =>
          import('./audit-logs/audit-logs.component')
            .then(m => m.AuditLogsComponent)
      }
    ]
  }
];