import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class CampaignService {
  private base = 'http://localhost:5234/api/campaign';
  constructor(private http: HttpClient) {}

  getCampaigns(): Observable<any[]> { return this.http.get<any[]>(this.base); }
  getCampaign(id: number): Observable<any> { return this.http.get(`${this.base}/${id}`); }
  createCampaign(data: any): Observable<any> { return this.http.post(this.base, data); }
  updateCampaign(id: number, data: any): Observable<any> { return this.http.put(`${this.base}/${id}`, data); }
  closeCampaign(id: number): Observable<any> { return this.http.post(`${this.base}/${id}/close`, {}); }
}