// import { Injectable } from '@angular/core';
// import { HttpClient } from '@angular/common/http';
// import { Observable } from 'rxjs';
// import { map } from 'rxjs/operators';
// import { Product } from '../models/product.model';

// @Injectable({
//   providedIn: 'root'
// })
// export class ApiService {

//   private baseUrl = 'http://localhost:5234/api';

//   constructor(private http: HttpClient) {}

//   createVisitor(consentStatus?: string): Observable<any> {
//     const payload = consentStatus
//       ? { consentStatus }
//       : {};

//     return this.http.post(
//       `${this.baseUrl}/visitor/create`,
//       payload
//     );
//   }

//   recordActivity(activity: any): Observable<any> {
//     return this.http.post(
//       `${this.baseUrl}/activity`,
//       activity
//     );
//   }

//   getProducts(): Observable<Product[]> {
//     return this.http.get<any[]>(
//       `${this.baseUrl}/product`
//     ).pipe(
//       map(products => products.map(product => this.normalizeProduct(product)))
//     );
//   }

//   getProduct(id: number): Observable<Product> {
//     return this.http.get<any>(
//       `${this.baseUrl}/product/${id}`
//     ).pipe(
//       map(product => this.normalizeProduct(product))
//     );
//   }

//   private normalizeProduct(product: any): Product {
//     return {
//       productId: product.productId ?? product.id,
//       name: product.name ?? 'Product',
//       description: product.description ?? '',
//       pricing: Number(product.pricing ?? 0),
//       features: this.parseList(product.features),
//       specifications: this.parseList(product.specifications),
//       status: product.status ?? 'Available'
//     };
//   }

//   private parseList(value: string | string[] | null | undefined): string[] {
//     if (Array.isArray(value)) {
//       return value.map(item => String(item).trim()).filter(Boolean);
//     }

//     if (!value) {
//       return [];
//     }

//     return String(value)
//       .split(/[|,;\n]/)
//       .map(item => item.trim())
//       .filter(Boolean);
//   }
// }

import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';  // <-- add HttpParams
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { Product } from '../models/product.model';
 
@Injectable({
  providedIn: 'root'
})
export class ApiService {

  private baseUrl = 'http://localhost:5234/api';

  constructor(private http: HttpClient) {}
 
  createVisitor(consentStatus?: string): Observable<any> {
    const payload = consentStatus ? { consentStatus } : {};
    return this.http.post(`${this.baseUrl}/visitor/create`, payload);
  }
 
  recordActivity(activity: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/activity`, activity);
  }
 
  // 🔧 Updated: accepts optional categoryId
  // getProducts(categoryId?: number): Observable<Product[]> {
  //   let params = new HttpParams();
  //   if (categoryId !== undefined && categoryId !== null) {
  //     params = params.set('categoryId', categoryId.toString());
  //   }
 
  //   return this.http.get<any[]>(
  //     `${this.baseUrl}/product`,
  //     { params }   // <-- pass query parameters
  //   ).pipe(
  //     map(products => products.map(product => this.normalizeProduct(product)))
  //   );
  // }
getProducts(categoryId?: number): Observable<Product[]> {

  let params = new HttpParams();

  if (
    categoryId !== undefined &&
    categoryId !== null
  ) {

    params = params.set(
      'categoryId',
      categoryId.toString()
    );

  }

  return this.http.get<any[]>(
    `${this.baseUrl}/product`,
    { params }
  ).pipe(

    map(products =>
      products.map(product =>
        this.normalizeProduct(product)
      )
    )

  );
}
 
  getProduct(id: number): Observable<Product> {
    return this.http.get<any>(
      `${this.baseUrl}/product/${id}`
    ).pipe(
      map(product => this.normalizeProduct(product))
    );
  }
 
  private normalizeProduct(product: any): Product {

  return {
    productId: product.productId ?? product.id,
    name: product.name ?? 'Product',
    description: product.description ?? '',
    pricing: Number(product.pricing ?? 0),
    features: this.parseList(product.features),
    specifications: this.parseList(product.specifications),
    status: product.status ?? 'Available',
    categoryId: product.categoryId ?? null,
    categoryName: product.categoryName ?? null
  };
}

 
  private parseList(value: string | string[] | null | undefined): string[] {
    if (Array.isArray(value)) {
      return value.map(item => String(item).trim()).filter(Boolean);
    }
    if (!value) return [];
    return String(value)
      .split(/[|,;\n]/)
      .map(item => item.trim())
      .filter(Boolean);
  }
}