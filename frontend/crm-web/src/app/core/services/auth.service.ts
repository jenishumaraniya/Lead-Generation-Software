import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap, of } from 'rxjs';
import { Router } from '@angular/router';

export interface UserProfile {
  userId?: number;
  employeeId?: number;
  fullName: string;
  email: string;
  role: 'ADMIN' | 'SALES_REP' | 'SALES';
  isActive?: boolean;
  createdAt?: string;
  lastLoginAt?: string;
  token?: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken?: string;
  expiresIn?: number;
  token?: string;
  role?: string;
  fullName?: string;
  email?: string;
  employeeId?: number;
  user: UserProfile;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = 'http://localhost:5234/api/auth';
  private currentUserSubject = new BehaviorSubject<UserProfile | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();
  public user$ = this.currentUser$;

  private ACCESS_TOKEN_KEY = 'crm_access_token';
  private REFRESH_TOKEN_KEY = 'crm_refresh_token';
  private USER_KEY = 'crm_user_profile';

  constructor(private http: HttpClient, private router: Router) {
    this.loadStoredUser();
  }

  private loadStoredUser(): void {
    const userJson = localStorage.getItem(this.USER_KEY);
    if (userJson) {
      try {
        const user = JSON.parse(userJson);
        this.currentUserSubject.next(user);
      } catch (e) {
        this.clearStorage();
      }
    }
  }

  public get currentUserValue(): UserProfile | null {
    return this.currentUserSubject.value;
  }

  public getCurrentUser(): UserProfile | null {
    return this.currentUserSubject.value;
  }

  public isAuthenticated(): boolean {
    return !!this.getAccessToken() && !!this.currentUserValue;
  }

  public isAdmin(): boolean {
    return this.currentUserValue?.role === 'ADMIN';
  }

  public isSalesRep(): boolean {
    return this.currentUserValue?.role === 'SALES_REP' || this.currentUserValue?.role === 'SALES';
  }

  public isSales(): boolean {
    return this.isSalesRep();
  }

  public hasRole(role: string): boolean {
    if (!this.currentUserValue) return false;
    if (role === 'SALES' || role === 'SALES_REP') {
      return this.currentUserValue.role === 'SALES' || this.currentUserValue.role === 'SALES_REP';
    }
    return this.currentUserValue.role === role;
  }

  public getAccessToken(): string | null {
    return localStorage.getItem(this.ACCESS_TOKEN_KEY);
  }

  public getToken(): string | null {
    return this.getAccessToken();
  }

  public getRefreshToken(): string | null {
    return localStorage.getItem(this.REFRESH_TOKEN_KEY);
  }

  login(credentialsOrEmail: any, password?: string): Observable<AuthResponse> {
    const creds = typeof credentialsOrEmail === 'string'
      ? { email: credentialsOrEmail, password: password || '' }
      : credentialsOrEmail;

    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, creds).pipe(
      tap(res => this.handleAuthResponse(res))
    );
  }

  refreshToken(): Observable<AuthResponse> {
    const refresh = this.getRefreshToken();
    return this.http.post<AuthResponse>(`${this.apiUrl}/refresh`, { refreshToken: refresh }).pipe(
      tap(res => this.handleAuthResponse(res))
    );
  }

  changePassword(data: { currentPassword: string; newPassword: string }): Observable<any> {
    return this.http.post(`${this.apiUrl}/change-password`, data);
  }

  forgotPassword(email: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/forgot-password`, { email });
  }

  resetPassword(data: { email: string; code: string; newPassword: string }): Observable<any> {
    return this.http.post(`${this.apiUrl}/reset-password`, data);
  }

  logout(): void {
    const refresh = this.getRefreshToken();
    if (refresh) {
      this.http.post(`${this.apiUrl}/logout`, { refreshToken: refresh }).subscribe({
        error: () => {}
      });
    }
    this.clearStorage();
    this.currentUserSubject.next(null);
    this.router.navigate(['/login']);
  }

  logoutAll(): void {
    this.http.post(`${this.apiUrl}/logout-all`, {}).subscribe({
      error: () => {}
    });
    this.clearStorage();
    this.currentUserSubject.next(null);
    this.router.navigate(['/login']);
  }

  private handleAuthResponse(res: AuthResponse): void {
    const token = res.accessToken || res.token || '';
    const userProfile: UserProfile = res.user || {
      fullName: res.fullName || 'User',
      email: res.email || '',
      role: ((res.role || 'SALES_REP') as any)
    };

    localStorage.setItem(this.ACCESS_TOKEN_KEY, token);
    if (res.refreshToken) {
      localStorage.setItem(this.REFRESH_TOKEN_KEY, res.refreshToken);
    }
    localStorage.setItem(this.USER_KEY, JSON.stringify(userProfile));
    localStorage.setItem('user', JSON.stringify(userProfile));
    this.currentUserSubject.next(userProfile);
  }

  private clearStorage(): void {
    localStorage.removeItem(this.ACCESS_TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    localStorage.removeItem('user');
  }
}
