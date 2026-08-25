import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';
@Component({
  selector: 'app-sales-layout',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './sales-layout.component.html',
  styleUrls: ['./sales-layout.component.css']
})
export class SalesLayoutComponent {
  user = this.authService.getCurrentUser();
  constructor(private authService: AuthService) {}
  logout() { this.authService.logout(); }
  get welcomeMessage(): string {
    return `Welcome, ${this.user?.fullName}`;
  }
  
}