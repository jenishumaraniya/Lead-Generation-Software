import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ScoreRule {
  scoreRuleId: number;
  name: string;
  eventType: string;
  category: 'INTENT' | 'ENGAGEMENT' | 'FIT' | 'ENRICHMENT' | 'COMPLIANCE' | string;
  direction: 'POSITIVE' | 'NEGATIVE';
  points: number;
  isActive: boolean;
  description?: string;
  createdAt?: string;
  updatedAt?: string;
}

@Injectable({
  providedIn: 'root'
})
export class RuleService {
  private apiUrl = 'http://localhost:5234/api/scoring/rules';

  constructor(private http: HttpClient) {}

  getRules(): Observable<ScoreRule[]> {
    return this.http.get<ScoreRule[]>(this.apiUrl);
  }

  getRule(id: number): Observable<ScoreRule> {
    return this.http.get<ScoreRule>(`${this.apiUrl}/${id}`);
  }

  createRule(rule: Partial<ScoreRule>): Observable<ScoreRule> {
    return this.http.post<ScoreRule>(this.apiUrl, rule);
  }

  updateRule(id: number, rule: Partial<ScoreRule>): Observable<ScoreRule> {
    return this.http.put<ScoreRule>(`${this.apiUrl}/${id}`, rule);
  }

  toggleRule(id: number): Observable<ScoreRule> {
    return this.http.post<ScoreRule>(`${this.apiUrl}/${id}/toggle`, {});
  }

  deleteRule(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }
}
