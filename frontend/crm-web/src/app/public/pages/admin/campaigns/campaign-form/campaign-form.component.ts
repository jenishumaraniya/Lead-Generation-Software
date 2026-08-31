import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  FormsModule,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { CampaignService } from '../../../../../core/services/campaign.service';

@Component({
  selector: 'app-campaign-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, RouterModule],
  templateUrl: './campaign-form.component.html',
  styleUrls: ['./campaign-form.component.css'],
})
export class CampaignFormComponent implements OnInit {
  form: FormGroup;
  isEdit = false;
  campaignId: number | null = null;

  allProspects: any[] = [];
  filteredProspects: any[] = [];
  selectedProspectIds: Set<number> = new Set<number>();
  prospectSearchTerm = '';
  csvUploadMessage = '';
  isUploadingCsv = false;
  saving = false;

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private campaignService: CampaignService,
  ) {
    this.form = this.fb.group({
      name: ['', Validators.required],
      description: [''],
      scheduleStartDate: [''],
      scheduleEndDate: [''],
      status: ['ACTIVE'],
      subject: [
        'Exploring opportunities with {{Company}}',
        Validators.required,
      ],
      body: [
        '<p>Hello {{Name}},</p><p>I noticed your work as {{JobTitle}} at {{Company}}. We provide enterprise solutions tailored for your industry.</p><p>Would you be open to a brief introductory conversation this week?</p><p>Best regards,<br/>Sales & Partnerships Team</p>',
        Validators.required,
      ],
      sendImmediately: [true],
    });
  }

  ngOnInit(): void {
    const id = this.route.snapshot.params['id'];
    if (id) {
      this.isEdit = true;
      this.campaignId = +id;
      this.campaignService.getCampaign(this.campaignId).subscribe({
        next: (data) => {
          const step1 = data.steps?.length ? data.steps[0] : null;
          this.form.patchValue({
            name: data.name || '',
            description: data.description || '',
            status: data.status || 'ACTIVE',
            scheduleStartDate: this.toInputDateTime(data.scheduleStartDate),
            scheduleEndDate: this.toInputDateTime(data.scheduleEndDate),
            subject:
              step1?.subject || 'Exploring opportunities with {{Company}}',
            body:
              step1?.body ||
              '<p>Hello {{Name}},</p><p>I noticed your work as {{JobTitle}} at {{Company}}. We provide enterprise solutions tailored for your industry.</p><p>Would you be open to a brief introductory conversation this week?</p><p>Best regards,<br/>Sales & Partnerships Team</p>',
            sendImmediately: false,
          });
          if (data.recipients?.length) {
            data.recipients.forEach((r: any) =>
              this.selectedProspectIds.add(r.prospectId),
            );
          }
        },
        error: () => this.router.navigate(['/admin/campaigns']),
      });
    }
    this.loadProspects();
  }

  private toInputDateTime(dateVal: any): string {
    if (!dateVal) return '';
    const d = new Date(dateVal);
    if (isNaN(d.getTime())) return '';
    const pad = (n: number) => (n < 10 ? '0' + n : '' + n);
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
  }

  get minDateTime(): string {
    const now = new Date();
    const year = now.getFullYear();
    const month = String(now.getMonth() + 1).padStart(2, '0');
    const day = String(now.getDate()).padStart(2, '0');
    const hours = String(now.getHours()).padStart(2, '0');
    const minutes = String(now.getMinutes()).padStart(2, '0');
    return `${year}-${month}-${day}T${hours}:${minutes}`;
  }

  loadProspects(): void {
    this.campaignService.getProspects().subscribe({
      next: (data) => {
        this.allProspects = data || [];
        this.applyProspectFilter();
      },
      error: () => {
        this.allProspects = [];
        this.filteredProspects = [];
      },
    });
  }

  applyProspectFilter(): void {
    if (!this.prospectSearchTerm) {
      this.filteredProspects = [...this.allProspects];
      return;
    }
    const term = this.prospectSearchTerm.toLowerCase();
    this.filteredProspects = this.allProspects.filter(
      (p) =>
        (p.name && p.name.toLowerCase().includes(term)) ||
        (p.email && p.email.toLowerCase().includes(term)) ||
        (p.jobTitle && p.jobTitle.toLowerCase().includes(term)) ||
        (p.company?.name && p.company.name.toLowerCase().includes(term)),
    );
  }

  toggleProspectSelection(prospectId: number): void {
    this.selectedProspectIds.has(prospectId)
      ? this.selectedProspectIds.delete(prospectId)
      : this.selectedProspectIds.add(prospectId);
  }

  toggleSelectAll(event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.filteredProspects.forEach((p) =>
      checked
        ? this.selectedProspectIds.add(p.prospectId)
        : this.selectedProspectIds.delete(p.prospectId),
    );
  }

  isAllSelected(): boolean {
    return (
      this.filteredProspects.length > 0 &&
      this.filteredProspects.every((p) =>
        this.selectedProspectIds.has(p.prospectId),
      )
    );
  }

  downloadTemplate(): void {
    window.open(this.campaignService.downloadCsvTemplateUrl(), '_blank');
  }

  onCsvFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;
    const file = input.files[0];
    const formData = new FormData();
    formData.append('file', file);

    this.isUploadingCsv = true;
    this.csvUploadMessage = 'Uploading and processing CSV...';

    this.campaignService.uploadProspectsCsv(formData).subscribe({
      next: (res) => {
        this.isUploadingCsv = false;
        this.csvUploadMessage = `CSV Processed: ${res.added} added, ${res.updated} updated. Total: ${res.totalProcessed}.`;
        this.loadProspects();
        if (res.prospects?.length) {
          res.prospects.forEach((p: any) =>
            this.selectedProspectIds.add(p.prospectId),
          );
        }
        input.value = '';
      },
      error: (err) => {
        this.isUploadingCsv = false;
        this.csvUploadMessage = err.error?.error || 'Failed to upload CSV.';
        input.value = '';
      },
    });
  }

  onSubmit(): void {
    console.log('=== onSubmit called ===');
    console.log('Form valid?', this.form.valid);
    console.log('Form value:', this.form.value);

    if (this.form.invalid) {
      console.warn('Form is invalid – returning.');
      return;
    }

    this.saving = true;
    const formVal = this.form.value;

    const payload = {
      name: formVal.name?.trim(),
      description: formVal.description?.trim() || null,
      status: formVal.status || 'ACTIVE',
      scheduleStartDate: formVal.scheduleStartDate
        ? new Date(formVal.scheduleStartDate).toISOString()
        : null,
      scheduleEndDate: formVal.scheduleEndDate
        ? new Date(formVal.scheduleEndDate).toISOString()
        : null,
      steps: [
        {
          stepNumber: 1,
          name: 'Initial Outreach',
          subject:
            formVal.subject?.trim() ||
            `Exploring opportunities - ${formVal.name}`,
          body:
            formVal.body?.trim() ||
            '<p>Hello {{Name}},</p><p>We wanted to reach out regarding solutions for {{Company}}.</p>',
          delayDays: 0,
          delayHours: 0,
        },
      ],
      prospectIds: Array.from(this.selectedProspectIds), // <-- CRITICAL: send all selected IDs
    };

    console.log('🔸 Payload:', payload);

    const obs = this.isEdit
      ? this.campaignService.updateCampaign(this.campaignId!, payload)
      : this.campaignService.createCampaign(payload);

    obs.subscribe({
      next: (campaign) => {
        console.log('✅ Campaign saved:', campaign);
        const id = this.isEdit ? this.campaignId! : campaign.campaignId;

        // Backend now handles recipient sync based on prospectIds – no manual enrollment needed.
        // Just proceed to launch if required.
        this.handlePostEnrollment(id, formVal);
      },
      error: (err) => {
        this.saving = false;
        console.error('❌ HTTP error:', err);
        alert('Failed to save campaign: ' + (err.message || 'Unknown error'));
      },
    });
  }

  private handlePostEnrollment(campaignId: number, formVal: any): void {
    console.log('🔁 handlePostEnrollment called');
    const shouldLaunch = formVal.sendImmediately || formVal.status === 'ACTIVE';
    console.log('🚀 Should launch?', shouldLaunch);

    if (shouldLaunch) {
      console.log('🚀 Launching campaign...');
      this.campaignService.launchCampaign(campaignId).subscribe({
        next: (res) => {
          console.log('✅ Launch succeeded:', res);
          this.saving = false;
          this.router.navigate(['/admin/campaigns']);
        },
        error: (err) => {
          console.error('❌ Launch failed:', err);
          this.saving = false;
          alert('Campaign saved but launch failed: ' + err.message);
          this.router.navigate(['/admin/campaigns']);
        },
      });
    } else {
      console.log(
        'ℹ️ Skipping launch (sendImmediately=false, status != ACTIVE).',
      );
      this.saving = false;
      this.router.navigate(['/admin/campaigns']);
    }
  }

  goBack(): void {
    this.router.navigate(['/admin/campaigns']);
  }
}
