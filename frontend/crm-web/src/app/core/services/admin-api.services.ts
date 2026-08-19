import { Injectable } from '@angular/core'; 

import { HttpClient } from '@angular/common/http'; 

 

@Injectable({ 

  providedIn: 'root' 

}) 

export class AdminApiService { 

 

  private baseUrl = 

    'http://localhost:5234/api/admin'; 

 

  constructor(private http: HttpClient) {} 

 

  getVisitors() { 

    return this.http.get<any[]>( 

      `${this.baseUrl}/visitors` 

    ); 

  } 

 

  getVisitorDetails( 

    anonymousId: string 

  ) { 

    return this.http.get<any>( 

      `${this.baseUrl}/visitors/${anonymousId}` 

    ); 

  } 

} 