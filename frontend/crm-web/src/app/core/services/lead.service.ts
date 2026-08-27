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
  status: string; // NEW, CONTACTED, QUALIFIED, WON, LOST
  score?: number;
  qualification?: string;
  assignedTo: number | null;
  assignedSalespersonName?: string | null;
  assignedSalespersonEmail?: string | null;
  assignedCategoryName?: string | null;
  isMultiCategory?: boolean;
  productIds?: number[];
  quantity?: number;
  timeline?: string;
  businessRequirement?: string;
  source?: string;
  createdAt: string;
  updatedAt?: string;
  nextFollowUpDate: string | null;
  notes: string;
  visitor?: any;
  prospect?: any;
  scoreHistories?: any[];
  statusHistories?: any[];
  notesList?: any[];
  activities?: any[];
}

@Injectable({ providedIn: 'root' })
export class LeadService {
  private base = 'http://localhost:5234/api/lead';

  constructor(private http: HttpClient) {}

  getLeads(assignedTo?: number): Observable<Lead[]> {
    const url = assignedTo ? `${this.base}?assignedTo=${assignedTo}` : this.base;
    return this.http.get<Lead[]>(url);
  }

  getLead(id: number): Observable<Lead> {
    return this.http.get<Lead>(`${this.base}/${id}`);
  }

  updateLead(id: number, data: { status?: string; qualification?: string; score?: number; assignedTo?: number | null; nextFollowUpDate?: string | null; notes?: string }): Observable<any> {
    return this.http.put(`${this.base}/${id}`, data);
  }

  addActivity(id: number, activity: { activityType: string; description: string }): Observable<any> {
    return this.http.post(`${this.base}/${id}/activity`, activity);
  }

  getActivities(id: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.base}/${id}/activities`);
  }

  addNote(id: number, noteText: string): Observable<any> {
    return this.http.post(`${this.base}/${id}/notes`, { noteText });
  }

  getNotes(id: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.base}/${id}/notes`);
  }

  assignLead(id: number, employeeId: number | null): Observable<any> {
    return this.http.post(`${this.base}/${id}/assign`, { employeeId: employeeId || 0 });
  }
}