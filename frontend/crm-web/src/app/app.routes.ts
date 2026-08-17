import { Routes } from '@angular/router';

import { HomeComponent } from './public/home/home.component';
import { ProductListComponent } from './public/products/product-list/product-list.component';
import { ProductDetailsComponent } from './public/products/product-details/product-details.component';
import { CompareComponent } from './public/products/compare/compare.component';
import { PricingComponent } from './public/pricing/pricing.component';

export const routes: Routes = [

  {
    path: '',
    component: HomeComponent
  },

  {
    path: 'products',
    component: ProductListComponent
  },

  {
    path: 'products/:id',
    component: ProductDetailsComponent
  },

  {
    path: 'compare',
    component: CompareComponent
  },

  {
    path: 'pricing',
    component: PricingComponent
  },

  {
    path: '**',
    redirectTo: ''
  }

];