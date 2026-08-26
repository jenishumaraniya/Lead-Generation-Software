import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService, UserProfile } from '../../app/core/services/auth.service';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './admin-layout.component.html',
  styleUrls: ['./admin-layout.component.css']
})
export class AdminLayoutComponent implements OnInit {
  currentUser: UserProfile | null = null;
  showChangePasswordModal = false;
  changePasswordData = {
    currentPassword: '',
    newPassword: '',
    confirmPassword: ''
  };
  changePasswordError = '';
  changePasswordSuccess = '';

  constructor(
    public authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
    });
  }

  logout(): void {
    this.authService.logout();
  }

  logoutAll(): void {
    if (confirm('Are you sure you want to log out of all active devices?')) {
      this.authService.logoutAll();
    }
  }

  quickSwitchRole(): void {
    if (this.authService.isAdmin()) {
      this.authService.login({ email: 'sales@leadgen.com', password: 'Sales@123' }).subscribe();
    } else {
      this.authService.login({ email: 'admin@leadgen.com', password: 'Admin@123' }).subscribe();
    }
  }

  openChangePassword(): void {
    this.changePasswordData = { currentPassword: '', newPassword: '', confirmPassword: '' };
    this.changePasswordError = '';
    this.changePasswordSuccess = '';
    this.showChangePasswordModal = true;
  }

  closeChangePassword(): void {
    this.showChangePasswordModal = false;
  }

  submitChangePassword(): void {
    if (!this.changePasswordData.currentPassword || !this.changePasswordData.newPassword) {
      this.changePasswordError = 'Please fill out all fields.';
      return;
    }

    if (this.changePasswordData.newPassword.length < 6) {
      this.changePasswordError = 'New password must be at least 6 characters long.';
      return;
    }

    if (this.changePasswordData.newPassword !== this.changePasswordData.confirmPassword) {
      this.changePasswordError = 'New passwords do not match.';
      return;
    }

    this.authService.changePassword({
      currentPassword: this.changePasswordData.currentPassword,
      newPassword: this.changePasswordData.newPassword
    }).subscribe({
      next: () => {
        this.changePasswordSuccess = 'Password updated successfully!';
        setTimeout(() => this.closeChangePassword(), 1500);
      },
      error: (err) => {
        this.changePasswordError = err.error?.error || 'Failed to change password. Check current password.';
      }
    });
  }
}
