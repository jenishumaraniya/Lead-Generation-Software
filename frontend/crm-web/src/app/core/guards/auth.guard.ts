import { inject, Injectable } from '@angular/core';
import { CanActivateFn, CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return true;
  }

  router.navigate(['/admin/login'], { queryParams: { returnUrl: state.url } });
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
      this.router.navigate(['/admin/login'], { queryParams: { returnUrl: state.url } });
      return false;
    }

    if (expectedRole && !this.authService.hasRole(expectedRole)) {
      if (this.authService.isAdmin()) {
        return true;
      }
      this.router.navigate(['/admin/dashboard']);
      return false;
    }

    return true;
  }
}
