import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class EmployeeService {
  private base = 'http://localhost:5234/api/users';

  constructor(private http: HttpClient) {}

  getEmployees(): Observable<any[]> {
    return this.http.get<any[]>(this.base).pipe(
      map(users => users.map(u => ({
        ...u,
        employeeId: u.userId,
        fullName: u.fullName || u.email
      })))
    );
  }
}