import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  RuleService,
  ScoreRule,
} from '../../../../../core/services/rule.service';

@Component({
  selector: 'app-rule-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './rule-list.component.html',
  styleUrls: ['./rule-list.component.css'],
})
export class RuleListComponent implements OnInit {
  rules: ScoreRule[] = [];
  filteredRules: ScoreRule[] = [];
  loading = false;
  successMessage = '';
  errorMessage = '';

  // Filter
  searchQuery = '';
  categoryFilter = '';
  statusFilter = '';

  // Stats
  totalRulesCount = 0;
  activeRulesCount = 0;
  positiveRulesCount = 0;
  penaltyRulesCount = 0;

  // Modal / Form state
  showModal = false;
  isEditing = false;
  selectedRuleId: number | null = null;
  ruleForm: Partial<ScoreRule> = {
    name: '',
    eventType: '',
    category: 'INTENT',
    points: 10,
    isActive: true,
    description: '',
  };

  categoriesList = ['INTENT', 'ENGAGEMENT', 'FIT', 'ENRICHMENT', 'COMPLIANCE'];

  // ===== NEW: For Event Type Dropdown =====
  eventTypes: string[] = [];
  showCustomEventInput = false;
  customEventType = '';
  // ========================================

  constructor(private ruleService: RuleService) {}

  ngOnInit(): void {
    this.loadRules();
    this.loadEventTypes();
  }

  loadRules(): void {
    this.loading = true;
    this.ruleService.getRules().subscribe({
      next: (data) => {
        this.rules = data || [];
        this.applyFilter();
        this.computeStats();
        this.loading = false;
      },
      error: (err) => {
        this.errorMessage = 'Failed to load lead qualification rules.';
        this.loading = false;
      },
    });
  }

  // ===== NEW: Load existing event types from API =====
  loadEventTypes(): void {
    this.ruleService.getEventTypes().subscribe({
      next: (data) => {
        this.eventTypes = data || [];
      },
      error: (err) => {
        console.error('Failed to load event types', err);
        this.eventTypes = [];
      },
    });
  }

  // ===== NEW: Compute available event types (exclude used ones) =====
  get availableEventTypes(): string[] {
    const usedEventTypes = new Set<string>();

    // Collect all event types from existing rules
    this.rules.forEach((rule) => {
      // If editing, exclude the current rule's own event type so it can still be selected
      if (this.isEditing && this.selectedRuleId === rule.scoreRuleId) {
        return; // skip adding this rule's event type to used set
      }
      usedEventTypes.add(rule.eventType);
    });

    // Filter the full list to only those not used
    const available = this.eventTypes.filter((et) => !usedEventTypes.has(et));

    // If editing, ensure the current rule's event type is in the list (it might have been filtered out)
    if (this.isEditing && this.ruleForm.eventType) {
      if (!available.includes(this.ruleForm.eventType)) {
        available.push(this.ruleForm.eventType);
      }
    }

    return available;
  }

  // ===== NEW: Handle dropdown selection =====
  onEventTypeChange(value: string): void {
    if (value === 'CUSTOM') {
      this.showCustomEventInput = true;
      this.ruleForm.eventType = '';
    } else {
      this.showCustomEventInput = false;
      this.ruleForm.eventType = value;
    }
  }

  // ===== NEW: Handle custom event type input =====
  onCustomEventTypeChange(value: string): void {
    this.ruleForm.eventType = value.toUpperCase().trim();
  }

  computeStats(): void {
    this.totalRulesCount = this.rules.length;
    this.activeRulesCount = this.rules.filter((r) => r.isActive).length;
    this.positiveRulesCount = this.rules.filter((r) => r.points > 0).length;
    this.penaltyRulesCount = this.rules.filter((r) => r.points < 0).length;
  }

  applyFilter(): void {
    this.filteredRules = this.rules.filter((rule) => {
      const matchesSearch =
        !this.searchQuery ||
        rule.name.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
        rule.eventType.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
        (rule.description &&
          rule.description
            .toLowerCase()
            .includes(this.searchQuery.toLowerCase()));

      const matchesCat =
        !this.categoryFilter || rule.category === this.categoryFilter;
      const matchesStatus =
        !this.statusFilter ||
        (this.statusFilter === 'active' && rule.isActive) ||
        (this.statusFilter === 'inactive' && !rule.isActive);

      return matchesSearch && matchesCat && matchesStatus;
    });
  }

  openCreateModal(): void {
    this.isEditing = false;
    this.selectedRuleId = null;
    this.ruleForm = {
      name: '',
      eventType: '',
      category: 'INTENT',
      points: 15,
      isActive: true,
      description: '',
    };
    this.showCustomEventInput = false;
    this.customEventType = '';
    this.errorMessage = '';
    this.showModal = true;
  }

  openEditModal(rule: ScoreRule): void {
    this.isEditing = true;
    this.selectedRuleId = rule.scoreRuleId;
    this.ruleForm = {
      name: rule.name,
      eventType: rule.eventType,
      category: rule.category,
      points: rule.points,
      isActive: rule.isActive,
      description: rule.description || '',
    };
    this.showCustomEventInput = false;
    this.customEventType = '';
    this.errorMessage = '';
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
    this.selectedRuleId = null;
    this.errorMessage = '';
    this.showCustomEventInput = false;
    this.customEventType = '';
  }

  saveRule(): void {
    if (!this.ruleForm.name?.trim() || !this.ruleForm.eventType?.trim()) {
      this.errorMessage = 'Rule Name and Event Type are required.';
      return;
    }

    const payload = {
      name: this.ruleForm.name.trim(),
      eventType: this.ruleForm.eventType.trim().toUpperCase(),
      category: this.ruleForm.category,
      points: Number(this.ruleForm.points || 0),
      isActive: !!this.ruleForm.isActive,
      description: this.ruleForm.description?.trim() || '',
    };

    if (this.isEditing && this.selectedRuleId) {
      this.ruleService.updateRule(this.selectedRuleId, payload).subscribe({
        next: () => {
          this.showToast('Rule updated successfully.');
          this.closeModal();
          this.loadRules();
          this.loadEventTypes();
        },
        error: (err) => {
          this.errorMessage = err.error?.error || 'Failed to update rule.';
        },
      });
    } else {
      this.ruleService.createRule(payload).subscribe({
        next: () => {
          this.showToast('New lead rule created successfully.');
          this.closeModal();
          this.loadRules();
          this.loadEventTypes();
        },
        error: (err) => {
          this.errorMessage = err.error?.error || 'Failed to create rule.';
        },
      });
    }
  }

  toggleActive(rule: ScoreRule): void {
    this.ruleService.toggleRule(rule.scoreRuleId).subscribe({
      next: (updated) => {
        rule.isActive = updated.isActive;
        this.computeStats();
        this.showToast(
          `Rule '${rule.name}' is now ${rule.isActive ? 'Active' : 'Inactive'}.`,
        );
      },
      error: () => {
        this.showToast('Failed to toggle rule status.');
      },
    });
  }

  deleteRule(rule: ScoreRule): void {
    if (!confirm(`Are you sure you want to delete the rule "${rule.name}"?`)) {
      return;
    }

    this.ruleService.deleteRule(rule.scoreRuleId).subscribe({
      next: () => {
        this.showToast(`Rule "${rule.name}" deleted.`);
        this.loadRules();
        this.loadEventTypes();
      },
      error: () => {
        this.showToast('Failed to delete rule.');
      },
    });
  }

  showToast(msg: string): void {
    this.successMessage = msg;
    setTimeout(() => {
      if (this.successMessage === msg) {
        this.successMessage = '';
      }
    }, 4000);
  }
}
