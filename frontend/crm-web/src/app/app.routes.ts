import { Routes } from '@angular/router'; 

export const routes: Routes = [ 
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
  {
    path: 'compare',
    loadComponent: () =>
      import('./public/products/compare/compare.component')
        .then(m => m.CompareComponent)
  },
  { 
    path: 'admin', 
    loadChildren: () => 
      import('../admin/admin.routes') 
        .then(m => m.ADMIN_ROUTES) 
  },
  { path: '**', redirectTo: '' }
];