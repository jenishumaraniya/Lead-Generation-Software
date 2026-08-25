// 


import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, of, tap, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { User } from '../models/user.model';
import { AuthResponse } from '../models/auth-response.model';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private baseUrl = 'http://localhost:5234/api/auth';
  private userSubject = new BehaviorSubject<User | null>(null);
  user$ = this.userSubject.asObservable();

  // ✅ Mock users for testing
  private mockUsers = [
    {
      email: 'admin@leadgen.com',
      password: 'admin123',
      role: 'ADMIN',
      fullName: 'Admin User',
      employeeId: 1
    },
    {
      email: 'sales@leadgen.com',
      password: 'sales123',
      role: 'SALES',
      fullName: 'Sales User',
      employeeId: 2
    }
  ];

  constructor(private http: HttpClient, private router: Router) {
    this.loadUserFromStorage();
  }

  private loadUserFromStorage(): void {
    const userData = localStorage.getItem('user');
    if (userData) {
      try {
        const user = JSON.parse(userData);
        this.userSubject.next(user);
      } catch {
        this.logout();
      }
    }
  }

  // ✅ Login with Mock or Real Backend
  login(email: string, password: string): Observable<AuthResponse> {
    // Check if mock user exists
    const mockUser = this.mockUsers.find(
      u => u.email === email && u.password === password
    );

    if (mockUser) {
      // Return mock response
      const response: AuthResponse = {
        token: 'mock-jwt-token-' + Date.now(),
        role: mockUser.role,
        fullName: mockUser.fullName,
        email: mockUser.email,
        employeeId: mockUser.employeeId
      };

      const user: User = {
        employeeId: response.employeeId,
        fullName: response.fullName,
        email: response.email,
        role: response.role as 'ADMIN' | 'SALES',
        token: response.token
      };

      localStorage.setItem('user', JSON.stringify(user));
      this.userSubject.next(user);

      return of(response);
    }

    // If not mock, try real backend (optional)
    // return this.http.post<AuthResponse>(`${this.baseUrl}/login`, { email, password })
    //   .pipe(tap(response => { ... }));

    // If no mock and no backend, return error
    return throwError(() => ({
      status: 401,
      error: { message: 'Invalid email or password. Try admin@leadgen.com / admin123' }
    }));
  }

  logout(): void {
    localStorage.removeItem('user');
    this.userSubject.next(null);
    this.router.navigate(['/login']);
  }

  getCurrentUser(): User | null {
    return this.userSubject.value;
  }

  isAuthenticated(): boolean {
    return this.userSubject.value !== null;
  }

  hasRole(role: 'ADMIN' | 'SALES'): boolean {
    const user = this.userSubject.value;
    return user ? user.role === role : false;
  }

  isAdmin(): boolean {
    return this.hasRole('ADMIN');
  }

  isSales(): boolean {
    return this.hasRole('SALES');
  }

  getToken(): string | null {
    return this.userSubject.value?.token || null;
  }
}