import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private base = `${environment.apiUrl}/product`;
  constructor(private http: HttpClient) {}

  getProducts(includeInactive: boolean = true): Observable<any[]> { 
    return this.http.get<any[]>(`${this.base}?includeInactive=${includeInactive}`); 
  }
  getProduct(id: number): Observable<any> { return this.http.get(`${this.base}/${id}`); }
  createProduct(data: any): Observable<any> { return this.http.post(this.base, data); }
  updateProduct(id: number, data: any): Observable<any> { return this.http.put(`${this.base}/${id}`, data); }
  updateProductStatus(id: number, status: string): Observable<any> { return this.http.post(`${this.base}/${id}/status`, { status }); }
  deleteProduct(id: number): Observable<any> { return this.http.delete(`${this.base}/${id}`); }
  uploadProductImage(file: File): Observable<{ imageUrl: string; message?: string }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ imageUrl: string; message?: string }>(`${this.base}/upload-image`, formData);
  }
}