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
  user = this.authService.getCurrentUser();
  constructor(private authService: AuthService) {}
  logout() { this.authService.logout(); }

  get welcomeMessage(): string {
    return `Welcome, ${this.user?.fullName}`;
  }

}