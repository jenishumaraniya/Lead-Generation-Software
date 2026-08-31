import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AIService {
  private baseUrl = 'http://localhost:5234/api/ai'; // adjust to your backend URL

  constructor(private http: HttpClient) {}

  // Trigger AI analysis for a lead
  analyzeLead(leadId: number): Observable<any> {
    return this.http.post(`${this.baseUrl}/analyze/${leadId}`, {});
  }

  // Retrieve existing analysis for a lead
  getAnalysis(leadId: number): Observable<any> {
    return this.http.get(`${this.baseUrl}/lead/${leadId}`);
  }

  // Get analysis history (optional)
  getAnalysisHistory(leadId: number): Observable<any> {
    return this.http.get(`${this.baseUrl}/history/${leadId}`);
  }
}
