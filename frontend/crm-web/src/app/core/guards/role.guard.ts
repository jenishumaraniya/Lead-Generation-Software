import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const roleGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const expectedRole = route.data?.['role'];

  if (!authService.isAuthenticated()) {
    router.navigate(['/admin/login']);
    return false;
  }

  if (expectedRole && !authService.hasRole(expectedRole)) {
    router.navigate(['/admin/dashboard']);
    return false;
  }

  return true;
};
