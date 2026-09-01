import { Component, OnInit } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';
import { SidebarService } from '../../../core/services/sidebar.service';
import { ChangePasswordModalComponent } from '../../../components/change-password-modal/change-password-modal.component';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, ChangePasswordModalComponent],
  templateUrl: './admin-layout.component.html',
  styleUrls: ['./admin-layout.component.css']
})
export class AdminLayoutComponent implements OnInit {
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
    const name = this.user?.fullName || 'Administrator';
    return name;
  }
}