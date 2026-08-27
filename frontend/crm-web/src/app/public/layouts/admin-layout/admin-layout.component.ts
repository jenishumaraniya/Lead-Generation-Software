import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './admin-layout.component.html',
  styleUrls: ['./admin-layout.component.css']
})
export class AdminLayoutComponent {
  constructor(private authService: AuthService) {}

  get user() {
    return this.authService.getCurrentUser();
  }

  logout() { 
    this.authService.logout(); 
  }

  get welcomeMessage(): string {
    const name = this.user?.fullName || 'Administrator';
    return `Welcome, ${name}`;
  }
}