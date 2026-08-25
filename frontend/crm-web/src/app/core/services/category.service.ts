import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Category } from '../models/category.model';

@Injectable({ providedIn: 'root' })
export class CategoryService {
  private base = 'http://localhost:5234/api/category'; // adjust to your backend

  constructor(private http: HttpClient) {}

  getCategories(): Observable<Category[]> {
    return this.http.get<Category[]>(this.base);
  }

  getCategory(id: number): Observable<Category> {
    return this.http.get<Category>(`${this.base}/${id}`);
  }

  createCategory(data: { categoryName: string }): Observable<Category> {
    return this.http.post<Category>(this.base, data);
  }

  updateCategory(id: number, data: { categoryName: string }): Observable<Category> {
    return this.http.put<Category>(`${this.base}/${id}`, data);
  }

  deleteCategory(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}