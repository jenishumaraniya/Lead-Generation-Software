import { inject, Injectable } from '@angular/core';
import { CanActivateFn, CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return true;
  }

  router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
  return false;
};

@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate {
  constructor(private authService: AuthService, private router: Router) {}

  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): boolean {
    const user = this.authService.getCurrentUser();
    const expectedRole = route.data['role'] as 'ADMIN' | 'SALES_REP' | 'SALES' | undefined;

    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
      return false;
    }

    if (expectedRole) {
      if (this.authService.isAdmin()) {
        return true;
      }
      if ((expectedRole === 'SALES' || expectedRole === 'SALES_REP') && this.authService.isSalesRep()) {
        return true;
      }
      if (this.authService.hasRole(expectedRole)) {
        return true;
      }

      // If user is sales rep trying to access admin
      if (this.authService.isSalesRep()) {
        this.router.navigate(['/sales/dashboard']);
        return false;
      }

      this.router.navigate(['/login']);
      return false;
    }

    return true;
  }
}
