import { Component, OnInit } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';
import { SidebarService } from '../../../core/services/sidebar.service';
import { ChangePasswordModalComponent } from '../../../components/change-password-modal/change-password-modal.component';

@Component({
  selector: 'app-sales-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, ChangePasswordModalComponent],
  templateUrl: './sales-layout.component.html',
  styleUrls: ['./sales-layout.component.css']
})
export class SalesLayoutComponent implements OnInit {
  isSidebarCollapsed = false;
  showChangePassword = false;

  constructor(
    private authService: AuthService,
    private sidebarService: SidebarService
  ) {}

  ngOnInit(): void {
    this.sidebarService.isCollapsed$.subscribe(collapsed => {
      this.isSidebarCollapsed = collapsed;
    });
  }

  toggleSidebar(): void {
    this.sidebarService.toggle();
  }

  get user() {
    return this.authService.getCurrentUser();
  }

  logout(): void { 
    this.authService.logout(); 
  }

  get welcomeMessage(): string {
    return this.user?.fullName || 'Sales Representative';
  }
}