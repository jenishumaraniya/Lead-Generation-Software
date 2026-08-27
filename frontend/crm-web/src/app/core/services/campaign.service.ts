import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class CampaignService {
  private base = 'http://localhost:5234/api/campaigns';
  private prospectBase = 'http://localhost:5234/api/prospects';

  constructor(private http: HttpClient) {}

  getCampaigns(): Observable<any[]> { return this.http.get<any[]>(this.base); }
  getCampaign(id: number): Observable<any> { return this.http.get(`${this.base}/${id}`); }
  createCampaign(data: any): Observable<any> { return this.http.post(this.base, data); }
  updateCampaign(id: number, data: any): Observable<any> { return this.http.put(`${this.base}/${id}`, data); }
  closeCampaign(id: number): Observable<any> { return this.http.post(`${this.base}/${id}/close`, {}); }
  deleteCampaign(id: number): Observable<any> { return this.http.delete(`${this.base}/${id}`); }

  getProspects(): Observable<any[]> {
    return this.http.get<any[]>(this.prospectBase);
  }

  enrollProspect(campaignId: number, prospectId: number): Observable<any> {
    return this.http.post(`${this.base}/${campaignId}/enroll`, { prospectId });
  }

  launchCampaign(campaignId: number): Observable<any> {
    return this.http.post(`${this.base}/${campaignId}/launch`, {});
  }

  uploadProspectsCsv(formData: FormData): Observable<any> {
    return this.http.post(`${this.prospectBase}/upload-csv`, formData);
  }

  downloadCsvTemplateUrl(): string {
    return `${this.prospectBase}/template`;
  }
}