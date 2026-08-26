import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { catchError, switchMap, throwError } from 'rxjs';

export const jwtInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const token = authService.getAccessToken();

  let authReq = req;
  if (token && req.url.includes('http://localhost:5234/api')) {
    authReq = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      // If 401 and we have a refresh token, attempt token refresh (except for login/refresh requests)
      if (error.status === 401 && !req.url.includes('/api/auth/login') && !req.url.includes('/api/auth/refresh')) {
        const refresh = authService.getRefreshToken();
        if (refresh) {
          return authService.refreshToken().pipe(
            switchMap(res => {
              const retryReq = req.clone({
                setHeaders: {
                  Authorization: `Bearer ${res.accessToken}`
                }
              });
              return next(retryReq);
            }),
            catchError(refreshErr => {
              authService.logout();
              return throwError(() => refreshErr);
            })
          );
        } else {
          authService.logout();
        }
      }
      return throwError(() => error);
    })
  );
};
