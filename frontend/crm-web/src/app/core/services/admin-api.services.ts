import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AdminApiService {
  private baseUrl = 'http://localhost:5234/api';

  constructor(private http: HttpClient) {}

  // --- Visitors (Inbound) ---
  getVisitors(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/admin/visitors`);
  }

  getVisitorDetails(anonymousId: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/admin/visitors/${anonymousId}`);
  }

  // --- Prospects & Discovery ---
  getProspects(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/prospects`);
  }

  getProspect(id: number): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/prospects/${id}`);
  }

  createProspect(data: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/prospects`, data);
  }

  discoverProspects(criteria: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/prospects/discover`, criteria);
  }

  triggerLinkedInEnrichment(prospectId: number): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/prospects/${prospectId}/enrichment`, {});
  }

  getProspectEnrichment(prospectId: number): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/prospects/${prospectId}/enrichment`);
  }

  // --- Campaigns & Sequences ---
  getCampaigns(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/campaigns`);
  }

  getCampaign(id: number): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/campaigns/${id}`);
  }

  createCampaign(data: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/campaigns`, data);
  }

  enrollProspectInCampaign(campaignId: number, prospectId: number): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/campaigns/${campaignId}/enroll`, { prospectId });
  }

  pauseCampaign(campaignId: number): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/campaigns/${campaignId}/pause`, {});
  }

  resumeCampaign(campaignId: number): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/campaigns/${campaignId}/resume`, {});
  }

  sendSequenceEmail(recipientId: number): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/email/send`, { campaignRecipientId: recipientId });
  }

  // --- Leads & Qualification ---
  getLeads(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/lead`);
  }

  getLead(id: number): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/lead/${id}`);
  }

  qualifyLead(id: number): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/lead/${id}/qualify`, {});
  }

  getLeadScoreHistory(id: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/lead/${id}/score-history`);
  }

  updateLead(id: number, data: any): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/lead/${id}`, data);
  }

  // --- AI Analysis & Insights ---
  analyzeLeadAI(leadId: number): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/ai/analyze/${leadId}`, {});
  }

  getLeadAIAnalysis(leadId: number): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/ai/lead/${leadId}`);
  }

  // --- Scoring Rules ---
  getScoreRules(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/scoring/rules`);
  }

  updateScoreRule(ruleId: number, data: { points: number; isActive: boolean }): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/scoring/rules/${ruleId}`, data);
  }

  // --- Handoffs ---
  handoffLead(leadId: number, destination: string = 'SALES_CRM'): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/handoff/leads/${leadId}?destination=${destination}`, {});
  }

  getHandoffLogs(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/handoff/logs`);
  }

  retryHandoff(handoffId: number): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/handoff/retry/${handoffId}`, {});
  }

  // --- Products Management ---
  getProducts(categoryId?: number, includeInactive: boolean = true): Observable<any[]> {
    let url = `${this.baseUrl}/product?includeInactive=${includeInactive}`;
    if (categoryId) url += `&categoryId=${categoryId}`;
    return this.http.get<any[]>(url);
  }

  getProduct(id: number): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/product/${id}`);
  }

  createProduct(data: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/product`, data);
  }

  updateProduct(id: number, data: any): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/product/${id}`, data);
  }

  deleteProduct(id: number): Observable<any> {
    return this.http.delete<any>(`${this.baseUrl}/product/${id}`);
  }

  // --- Categories Management ---
  getCategories(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/categories`);
  }

  createCategory(data: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/categories`, data);
  }

  updateCategory(id: number, data: any): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/categories/${id}`, data);
  }

  deleteCategory(id: number): Observable<any> {
    return this.http.delete<any>(`${this.baseUrl}/categories/${id}`);
  }

  // --- User Management (Admin Only) ---
  getUsers(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/users`);
  }

  createUser(data: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/users`, data);
  }

  updateUser(id: number, data: any): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/users/${id}`, data);
  }

  deleteUser(id: number): Observable<any> {
    return this.http.delete<any>(`${this.baseUrl}/users/${id}`);
  }

  resetUserPassword(id: number, newPassword: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/users/${id}/reset-password`, { newPassword });
  }

  toggleUserStatus(id: number): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/users/${id}/toggle-status`, {});
  }

  // --- Audit Logs (Admin Only) ---
  getAuditLogs(limit: number = 100, search?: string): Observable<any[]> {
    let url = `${this.baseUrl}/audit-logs?limit=${limit}`;
    if (search) url += `&search=${encodeURIComponent(search)}`;
    return this.http.get<any[]>(url);
  }
}