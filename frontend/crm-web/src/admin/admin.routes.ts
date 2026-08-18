import { Routes } from '@angular/router'; 

 

export const ADMIN_ROUTES: Routes = [ 

 

 { 

   path: '', 

   loadComponent: () => 

     import('./visitor/visitor-list/visitor-list.component') 

       .then(m => m.VisitorListComponent) 

 }, 

 

 { 

   path: 'visitors/:id', 

   loadComponent: () => 

     import('./visitor/visitor-details/visitor-details.component') 

       .then(m => m.VisitorDetailsComponent) 

 } 

 

]; 