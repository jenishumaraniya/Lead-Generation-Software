import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminApiService } from '../../app/core/services/admin-api.services';

@Component({
  selector: 'app-lead-pipeline',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './lead-pipeline.component.html',
  styleUrls: ['./lead-pipeline.component.css']
})
export class LeadPipelineComponent implements OnInit {
  leads: any[] = [];
  filteredLeads: any[] = [];
  selectedStage = 'ALL';
  searchTerm = '';
  isLoading = false;

  selectedLead: any = null;
  selectedLeadAI: any = null;
  scoreHistory: any[] = [];
  showLeadDrawer = false;
  isAnalyzingAI = false;
  isHandingOff = false;

  constructor(private adminApi: AdminApiService) {}

  ngOnInit(): void {
    this.loadLeads();
  }

  loadLeads(): void {
    this.isLoading = true;
    this.adminApi.getLeads().subscribe({
      next: (data) => {
        this.leads = data;
        this.applyFilter();
        this.isLoading = false;
      },
      error: (err) => {
        console.error(err);
        this.isLoading = false;
      }
    });
  }

  setStage(stage: string): void {
    this.selectedStage = stage;
    this.applyFilter();
  }

  applyFilter(): void {
    this.filteredLeads = this.leads.filter(l => {
      const stageMatch = this.selectedStage === 'ALL' ||
        (this.selectedStage === 'SQL' && (l.qualification === 'SQL' || l.qualification === 'HOT')) ||
        l.qualification === this.selectedStage ||
        (this.selectedStage === 'COLD' && (!l.qualification || l.qualification === 'COLD'));

      const searchMatch = !this.searchTerm ||
        l.fullName?.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        l.companyName?.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        l.email?.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        l.jobTitle?.toLowerCase().includes(this.searchTerm.toLowerCase());

      return stageMatch && searchMatch;
    });
  }

  openLeadDetails(lead: any): void {
    this.showLeadDrawer = true;
    this.selectedLead = null;
    this.selectedLeadAI = null;
    this.scoreHistory = [];

    this.adminApi.getLead(lead.leadId).subscribe({
      next: (data) => {
        this.selectedLead = data;
        this.loadLeadAI(lead.leadId);
        this.loadScoreHistory(lead.leadId);
      },
      error: (err) => console.error(err)
    });
  }

  loadLeadAI(leadId: number): void {
    this.adminApi.getLeadAIAnalysis(leadId).subscribe({
      next: (ai) => {
        this.selectedLeadAI = ai;
      },
      error: () => {}
    });
  }

  loadScoreHistory(leadId: number): void {
    this.adminApi.getLeadScoreHistory(leadId).subscribe({
      next: (history) => {
        this.scoreHistory = history;
      },
      error: () => {}
    });
  }

  triggerAIAnalysis(): void {
    if (!this.selectedLead) return;
    this.isAnalyzingAI = true;

    this.adminApi.analyzeLeadAI(this.selectedLead.leadId).subscribe({
      next: () => {
        this.isAnalyzingAI = false;
        this.loadLeadAI(this.selectedLead.leadId);
      },
      error: (err) => {
        console.error(err);
        this.isAnalyzingAI = false;
      }
    });
  }

  triggerQualify(): void {
    if (!this.selectedLead) return;

    this.adminApi.qualifyLead(this.selectedLead.leadId).subscribe({
      next: (res) => {
        this.selectedLead.qualification = res.stage;
        this.selectedLead.score = res.score;
        this.loadLeads();
        this.loadScoreHistory(this.selectedLead.leadId);
      },
      error: (err) => console.error(err)
    });
  }

  handoffToCRM(): void {
    if (!this.selectedLead) return;
    this.isHandingOff = true;

    this.adminApi.handoffLead(this.selectedLead.leadId).subscribe({
      next: () => {
        this.isHandingOff = false;
        this.selectedLead.status = 'QUALIFIED_HANDOFF';
        this.loadLeads();
        alert(`Lead "${this.selectedLead.fullName}" successfully handed off to Sales CRM!`);
      },
      error: (err) => {
        this.isHandingOff = false;
        alert(err.error?.error || 'Failed to dispatch handoff.');
      }
    });
  }

  closeDrawer(): void {
    this.showLeadDrawer = false;
    this.selectedLead = null;
  }
}
