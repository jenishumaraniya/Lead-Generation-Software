import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate {
  constructor(private authService: AuthService, private router: Router) {}

  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): boolean {
    const user = this.authService.getCurrentUser();
    const expectedRole = route.data['role'] as 'ADMIN' | 'SALES' | undefined;

    if (!user) {
      this.router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
      return false;
    }

    // If route requires a specific role
    if (expectedRole && user.role !== expectedRole) {
      // If admin, can access everything; but if expected is SALES, and user is ADMIN, allow too.
      if (user.role === 'ADMIN') {
        return true;
      }
      this.router.navigate(['/']);
      return false;
    }

    return true;
  }
}