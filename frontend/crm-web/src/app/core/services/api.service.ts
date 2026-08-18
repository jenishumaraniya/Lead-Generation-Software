import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { Product } from '../models/product.model';

@Injectable({
  providedIn: 'root'
})
export class ApiService {

  private baseUrl = 'http://localhost:5234/api/product  ';

  constructor(private http: HttpClient) {}

  createVisitor(consentStatus?: string): Observable<any> {
    const payload = consentStatus
      ? { consentStatus }
      : {};

    return this.http.post(
      `${this.baseUrl}/visitor/create`,
      payload
    );
  }

  recordActivity(activity: any): Observable<any> {
    return this.http.post(
      `${this.baseUrl}/activity`,
      activity
    );
  }

  getProducts(): Observable<Product[]> {
    return this.http.get<Product[]>(
      `${this.baseUrl}/product`
    );
  }

  getProduct(id: number): Observable<Product> {
    return this.http.get<Product>(
      `${this.baseUrl}/product/${id}`
    );
  }
}