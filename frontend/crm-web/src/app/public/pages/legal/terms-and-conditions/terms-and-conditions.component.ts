import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-terms-and-conditions',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './terms-and-conditions.component.html',
  styleUrls: ['../privacy-policy/privacy-policy.component.css']
})
export class TermsAndConditionsComponent {
  lastUpdated = 'September 2026';
}
