import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';

export interface Salesperson {
  userId: number;
  employeeId: number;
  fullName: string;
  email: string;
  role: string;
  isActive: boolean;
  categoryId?: number | null;
  categoryName?: string | null;
  createdAt: string;
  lastLoginAt?: string | null;
}

@Injectable({ providedIn: 'root' })
export class EmployeeService {
  private base = 'http://localhost:5234/api/users';

  constructor(private http: HttpClient) {}

  getEmployees(): Observable<Salesperson[]> {
    return this.http.get<any[]>(this.base).pipe(
      map(users => users.map(u => ({
        ...u,
        employeeId: u.userId,
        fullName: u.fullName || u.email
      })))
    );
  }

  createSalesperson(data: { fullName: string; email: string; password: string; role?: string; categoryId?: number | null }): Observable<any> {
    return this.http.post<any>(this.base, { ...data, role: 'SALES_REP' });
  }

  updateSalesperson(id: number, data: { fullName: string; role?: string; isActive?: boolean; categoryId?: number | null }): Observable<any> {
    return this.http.put<any>(`${this.base}/${id}`, data);
  }

  assignCategory(userId: number, categoryId: number | null): Observable<any> {
    return this.http.put<any>(`${this.base}/${userId}/category`, { categoryId });
  }

  deleteSalesperson(id: number): Observable<any> {
    return this.http.delete(`${this.base}/${id}`);
  }

  resetPassword(id: number, newPassword: string): Observable<any> {
    return this.http.post(`${this.base}/${id}/reset-password`, { newPassword });
  }

  toggleStatus(id: number): Observable<any> {
    return this.http.post(`${this.base}/${id}/toggle-status`, {});
  }
}