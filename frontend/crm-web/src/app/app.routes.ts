import { Routes } from '@angular/router';
import { AuthGuard } from './core/guards/auth.guard';
import { LoginComponent } from './public/pages/login/login.component';

export const routes: Routes = [
  // =============================================
  // PUBLIC ROUTES (no authentication required)
  // =============================================
  { 
    path: '', 
    loadComponent: () => 
      import('./public/home/home.component') 
        .then(m => m.HomeComponent) 
  },
  { 
    path: 'products', 
    loadComponent: () => 
      import('./public/products/product-list/product-list.component') 
        .then(m => m.ProductListComponent) 
  },
  {
    path: 'products/:id',
    loadComponent: () =>
      import('./public/products/product-details/product-details.component')
        .then(m => m.ProductDetailsComponent)
  },

  // =============================================
  // LEGAL & COMPLIANCE PAGES
  // =============================================
  {
    path: 'privacy-policy',
    loadComponent: () =>
      import('./public/pages/legal/privacy-policy/privacy-policy.component')
        .then(m => m.PrivacyPolicyComponent)
  },
  {
    path: 'terms-and-conditions',
    loadComponent: () =>
      import('./public/pages/legal/terms-and-conditions/terms-and-conditions.component')
        .then(m => m.TermsAndConditionsComponent)
  },
  {
    path: 'terms',
    redirectTo: 'terms-and-conditions',
    pathMatch: 'full'
  },
  {
    path: 'security-compliance',
    loadComponent: () =>
      import('./public/pages/legal/security-compliance/security-compliance.component')
        .then(m => m.SecurityComplianceComponent)
  },

  // =============================================
  // AUTHENTICATION
  // =============================================
  { path: 'login', component: LoginComponent },
  { path: 'admin/login', redirectTo: 'login', pathMatch: 'full' },
  { path: 'sales/login', redirectTo: 'login', pathMatch: 'full' },
  { path: 'pricing', redirectTo: 'products', pathMatch: 'full' },
  { path: 'compare', redirectTo: 'products', pathMatch: 'full' },
  { path: 'products/compare', redirectTo: 'products', pathMatch: 'full' },

  // =============================================
  // ADMIN ROUTES (protected)
  // =============================================
  {
    path: 'admin',
    loadComponent: () => 
      import('./public/layouts/admin-layout/admin-layout.component')
        .then(m => m.AdminLayoutComponent),
    canActivate: [AuthGuard],
    data: { role: 'ADMIN' },
    children: [
      { 
        path: 'dashboard', 
        loadComponent: () => 
          import('./public/pages/admin/dashboard/dashboard.component')
            .then(m => m.AdminDashboardComponent) 
      },
      { 
        path: 'leads', 
        loadComponent: () => 
          import('./public/pages/admin/leads/lead-list/lead-list.component')
            .then(m => m.LeadListComponent) 
      },
      { 
        path: 'leads/:id', 
        loadComponent: () => 
          import('./public/pages/admin/leads/lead-detail/lead-detail.component')
            .then(m => m.LeadDetailComponent) 
      },
      { 
        path: 'campaigns', 
        loadComponent: () => 
          import('./public/pages/admin/campaigns/campaign-list/campaign-list.component')
            .then(m => m.CampaignListComponent) 
      },
      { 
        path: 'campaigns/new', 
        loadComponent: () => 
          import('./public/pages/admin/campaigns/campaign-form/campaign-form.component')
            .then(m => m.CampaignFormComponent) 
      },
      { 
        path: 'campaigns/:id', 
        loadComponent: () => 
          import('./public/pages/admin/campaigns/campaign-form/campaign-form.component')
            .then(m => m.CampaignFormComponent) 
      },
      { 
        path: 'products', 
        loadComponent: () => 
          import('./public/pages/admin/products/product-list/product-list.component')
            .then(m => m.ProductListComponent) 
      },
      { 
        path: 'salespersons', 
        loadComponent: () => 
          import('./public/pages/admin/salespersons/salesperson-list/salesperson-list.component')
            .then(m => m.SalespersonListComponent) 
      },
      {
        path: 'categories',
        loadComponent: () =>
          import('./public/pages/admin/categories/category-list/category-list.component')
            .then(m => m.CategoryListComponent)
      },
      {
        path: 'rules',
        loadComponent: () =>
          import('./public/pages/admin/rules/rule-list/rule-list.component')
            .then(m => m.RuleListComponent)
      },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
    ]
  },

  // =============================================
  // SALES ROUTES (protected)
  // =============================================
  {
    path: 'sales',
    loadComponent: () => 
      import('./public/layouts/sales-layout/sales-layout.component')
        .then(m => m.SalesLayoutComponent),
    canActivate: [AuthGuard],
    data: { role: 'SALES' },
    children: [
      { 
        path: 'dashboard', 
        loadComponent: () => 
          import('./public/pages/sales/dashboard/dashboard.component')
            .then(m => m.SalesDashboardComponent) 
      },
      { 
        path: 'leads', 
        loadComponent: () => 
          import('./public/pages/sales/leads/sales-lead-list/sales-lead-list.component')
            .then(m => m.SalesLeadListComponent) 
      },
      { 
        path: 'leads/:id', 
        loadComponent: () => 
          import('./public/pages/sales/leads/lead-detail/lead-detail.component')
            .then(m => m.LeadDetailComponent) 
      },
      {
        path: 'guidelines',
        loadComponent: () =>
          import('./public/pages/sales/guidelines/qualification-guidelines.component')
            .then(m => m.QualificationGuidelinesComponent)
      },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
    ]
  },

  // =============================================
  // WILDCARD – redirect to home
  // =============================================
  { path: '**', redirectTo: '' }
];