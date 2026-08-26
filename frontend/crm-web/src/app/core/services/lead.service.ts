import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Lead {
  leadId: number;
  companyName: string;
  fullName: string;
  email: string;
  jobTitle: string;
  domain: string;
  industry: string;
  country: string;
  phone: string;
  status: string; // NEW, CONTACTED, QUALIFIED, LOST, WON
  score?: number;
  qualification?: string;
  assignedTo: number | null;
  assignedEmployee?: { employeeId: number; fullName: string };
  createdAt: string;
  lastContactDate: string | null;
  nextFollowUpDate: string | null;
  notes: string;
}

@Injectable({ providedIn: 'root' })
export class LeadService {
  private base = 'http://localhost:5234/api/lead';

  constructor(private http: HttpClient) {}

  getLeads(): Observable<Lead[]> {
    return this.http.get<Lead[]>(this.base);
  }

  getLead(id: number): Observable<Lead> {
    return this.http.get<Lead>(`${this.base}/${id}`);
  }

  updateLead(id: number, data: any): Observable<any> {
    return this.http.put(`${this.base}/${id}`, data);
  }

  addActivity(id: number, activity: { activityType: string; description: string }): Observable<any> {
    return this.http.post(`${this.base}/${id}/activity`, activity);
  }

  getActivities(id: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.base}/${id}/activities`);
  }

  assignLead(id: number, employeeId: number): Observable<any> {
    return this.http.post(`${this.base}/${id}/assign`, { employeeId });
  }
}