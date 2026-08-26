import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminApiService } from '../../app/core/services/admin-api.services';

@Component({
  selector: 'app-scoring-hub',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './scoring-hub.component.html',
  styleUrls: ['./scoring-hub.component.css']
})
export class ScoringHubComponent implements OnInit {
  rules: any[] = [];
  isLoading = false;
  selectedCategory = 'ALL';
  categories = ['ALL', 'INTENT', 'FIT', 'ENGAGEMENT', 'ENRICHMENT', 'COMPLIANCE'];

  constructor(private adminApi: AdminApiService) {}

  ngOnInit(): void {
    this.loadRules();
  }

  loadRules(): void {
    this.isLoading = true;
    this.adminApi.getScoreRules().subscribe({
      next: (data) => {
        this.rules = data;
        this.isLoading = false;
      },
      error: (err) => {
        console.error(err);
        this.isLoading = false;
      }
    });
  }

  get filteredRules(): any[] {
    if (this.selectedCategory === 'ALL') return this.rules;
    return this.rules.filter(r => r.category === this.selectedCategory);
  }

  updateRule(rule: any): void {
    this.adminApi.updateScoreRule(rule.scoreRuleId, {
      points: Number(rule.points),
      isActive: rule.isActive
    }).subscribe({
      next: (updated) => {
        rule.points = updated.points;
        rule.isActive = updated.isActive;
      },
      error: (err) => alert(err.error?.error || 'Failed to update rule')
    });
  }

  adjustPoints(rule: any, delta: number): void {
    rule.points = (Number(rule.points) || 0) + delta;
    this.updateRule(rule);
  }

  toggleActive(rule: any): void {
    rule.isActive = !rule.isActive;
    this.updateRule(rule);
  }
}
