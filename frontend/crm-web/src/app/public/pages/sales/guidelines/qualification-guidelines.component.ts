import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-qualification-guidelines',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="guidelines-page">
      <div class="page-header">
        <div class="header-badge">SALES PLAYBOOK & SLA</div>
        <h1>🎯 Lead Qualification Framework & Guidelines</h1>
        <p class="subtitle">Standard operational criteria for qualifying, scoring, and advancing inbound and enriched B2B leads.</p>
      </div>

      <div class="grid-layout">
        <!-- Section 1: Lead Stages -->
        <div class="card">
          <div class="card-icon">🏷️</div>
          <h2>Lead Classification Stages</h2>
          <div class="stage-item">
            <span class="badge badge-cold">COLD (0-29 pts)</span>
            <p>Inbound inquiry or newly scraped contact with basic requirement. Unverified budget or generic company size.</p>
          </div>
          <div class="stage-item">
            <span class="badge badge-warm">WARM / MQL (30-59 pts)</span>
            <p>Verified decision maker (Manager, Lead, VP) with specific product inquiry, identified company headcount, and immediate timeline.</p>
          </div>
          <div class="stage-item">
            <span class="badge badge-hot">HOT / SQL (60+ pts)</span>
            <p>High-value opportunity with multi-unit volume requirement, verified corporate email, LinkedIn profile enriched, and active budget.</p>
          </div>
        </div>

        <!-- Section 2: BANT Framework -->
        <div class="card">
          <div class="card-icon">📋</div>
          <h2>The BANT Qualification Standard</h2>
          <ul class="guideline-list">
            <li><strong>Budget (B):</strong> Has the prospect confirmed budget allocation or requested volume pricing?</li>
            <li><strong>Authority (A):</strong> Is the contact a verified Key Decision Maker, CTO, IT Director, or Procurement Lead?</li>
            <li><strong>Need (N):</strong> Are hardware specifications (CPU, RAM, storage, form factor, quantity) clearly identified?</li>
            <li><strong>Timeline (T):</strong> Is deployment planned Immediately, Within 1 Week, or This Month?</li>
          </ul>
        </div>

        <!-- Section 3: SLA & Response Protocols -->
        <div class="card">
          <div class="card-icon">⚡</div>
          <h2>Response Time SLAs</h2>
          <div class="sla-row">
            <span class="sla-badge urgent">HOT / SQL</span>
            <span>Contact within <strong>15 minutes</strong> (Direct Phone / Custom Pitch)</span>
          </div>
          <div class="sla-row">
            <span class="sla-badge warning">WARM / MQL</span>
            <span>Contact within <strong>2 hours</strong> (Enriched Email + Spec Sheet)</span>
          </div>
          <div class="sla-row">
            <span class="sla-badge normal">COLD / NEW</span>
            <span>Enroll in <strong>Automated Campaign Sequence</strong></span>
          </div>
        </div>

        <!-- Section 4: AI Enrichment & Note Protocols -->
        <div class="card">
          <div class="card-icon">🧠</div>
          <h2>AI & Notes Best Practices</h2>
          <ul class="guideline-list">
            <li>Always click <strong>"Run AI Analysis"</strong> before placing an outbound call to review generated icebreakers and pain points.</li>
            <li>Log all call outcomes under <strong>"Add Note"</strong> immediately after each interaction.</li>
            <li>If requirement spans across multiple categories (e.g. Laptops + Servers), mark as <strong>Multi-Category</strong> to coordinate with admin.</li>
          </ul>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .guidelines-page {
      padding: 24px;
      max-width: 1200px;
      margin: 0 auto;
      font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
      color: #1e293b;
    }
    .page-header {
      margin-bottom: 28px;
    }
    .header-badge {
      font-size: 0.75rem;
      font-weight: 700;
      color: #2563eb;
      letter-spacing: 0.08em;
      margin-bottom: 6px;
    }
    .page-header h1 {
      font-size: 1.8rem;
      font-weight: 800;
      color: #0f172a;
      margin-bottom: 8px;
    }
    .subtitle {
      color: #64748b;
      font-size: 0.95rem;
      margin: 0;
    }
    .grid-layout {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(360px, 1fr));
      gap: 20px;
    }
    .card {
      background: #ffffff;
      border: 1px solid #e2e8f0;
      border-radius: 14px;
      padding: 24px;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.04);
    }
    .card-icon {
      font-size: 28px;
      margin-bottom: 12px;
    }
    .card h2 {
      font-size: 1.2rem;
      font-weight: 700;
      color: #0f172a;
      margin-bottom: 16px;
    }
    .stage-item {
      margin-bottom: 14px;
    }
    .stage-item p {
      font-size: 0.85rem;
      color: #475569;
      margin: 4px 0 0;
      line-height: 1.5;
    }
    .badge {
      display: inline-block;
      padding: 3px 8px;
      border-radius: 6px;
      font-size: 0.75rem;
      font-weight: 700;
    }
    .badge-cold { background: #f1f5f9; color: #475569; }
    .badge-warm { background: #eff6ff; color: #2563eb; }
    .badge-hot { background: #fef2f2; color: #dc2626; }
    .guideline-list {
      padding-left: 18px;
      margin: 0;
    }
    .guideline-list li {
      font-size: 0.85rem;
      color: #475569;
      margin-bottom: 10px;
      line-height: 1.5;
    }
    .sla-row {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 10px 0;
      border-bottom: 1px solid #f1f5f9;
      font-size: 0.85rem;
    }
    .sla-badge {
      padding: 3px 8px;
      border-radius: 4px;
      font-size: 0.7rem;
      font-weight: 700;
      min-width: 75px;
      text-align: center;
    }
    .sla-badge.urgent { background: #fee2e2; color: #991b1b; }
    .sla-badge.warning { background: #fef3c7; color: #92400e; }
    .sla-badge.normal { background: #e0f2fe; color: #075985; }
  `]
})
export class QualificationGuidelinesComponent {}
