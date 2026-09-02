import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-security-compliance',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './security-compliance.component.html',
  styleUrls: ['../privacy-policy/privacy-policy.component.css']
})
export class SecurityComplianceComponent {
  lastUpdated = 'September 2026';
}
