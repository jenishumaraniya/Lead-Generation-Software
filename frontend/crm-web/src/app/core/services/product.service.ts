import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private base = 'http://localhost:5234/api/product';
  constructor(private http: HttpClient) {}

  getProducts(includeInactive: boolean = true): Observable<any[]> { 
    return this.http.get<any[]>(`${this.base}?includeInactive=${includeInactive}`); 
  }
  getProduct(id: number): Observable<any> { return this.http.get(`${this.base}/${id}`); }
  createProduct(data: any): Observable<any> { return this.http.post(this.base, data); }
  updateProduct(id: number, data: any): Observable<any> { return this.http.put(`${this.base}/${id}`, data); }
  deleteProduct(id: number): Observable<any> { return this.http.delete(`${this.base}/${id}`); }
}