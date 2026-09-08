import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminApiService } from '../../app/core/services/admin-api.services';
import { AuthService } from '../../app/core/services/auth.service';

@Component({
  selector: 'app-user-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './user-management.component.html',
  styleUrls: ['./user-management.component.css']
})
export class UserManagementComponent implements OnInit {
  users: any[] = [];
  filteredUsers: any[] = [];
  searchTerm = '';
  roleFilter = 'ALL';
  isLoading = false;

  showCreateModal = false;
  showResetPasswordModal = false;
  selectedUserForReset: any = null;
  newPasswordToSet = '';

  userForm = {
    fullName: '',
    email: '',
    password: '',
    role: 'SALES_REP'
  };

  constructor(
    private adminApi: AdminApiService,
    public authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.isLoading = true;
    this.adminApi.getUsers().subscribe({
      next: (data) => {
        this.users = data;
        this.applyFilter();
        this.isLoading = false;
      },
      error: (err) => {
        console.error(err);
        this.isLoading = false;
      }
    });
  }

  applyFilter(): void {
    this.filteredUsers = this.users.filter(u => {
      const matchSearch = !this.searchTerm ||
        u.fullName.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        u.email.toLowerCase().includes(this.searchTerm.toLowerCase());

      const matchRole = this.roleFilter === 'ALL' || u.role === this.roleFilter;
      return matchSearch && matchRole;
    });
  }

  openCreateModal(): void {
    this.userForm = {
      fullName: '',
      email: '',
      password: '',
      role: 'SALES_REP'
    };
    this.showCreateModal = true;
  }

  closeCreateModal(): void {
    this.showCreateModal = false;
  }

  createUser(): void {
    if (!this.userForm.fullName || !this.userForm.email || !this.userForm.password) {
      alert('Please fill out all required fields.');
      return;
    }

    this.adminApi.createUser(this.userForm).subscribe({
      next: () => {
        this.closeCreateModal();
        this.loadUsers();
      },
      error: (err) => alert(err.error?.error || 'Failed to create user')
    });
  }

  toggleStatus(user: any): void {
    this.adminApi.toggleUserStatus(user.userId).subscribe({
      next: (updated) => user.isActive = updated.isActive,
      error: (err) => alert(err.error?.error || 'Failed to toggle status')
    });
  }

  openResetPasswordModal(user: any): void {
    this.selectedUserForReset = user;
    this.newPasswordToSet = '';
    this.showResetPasswordModal = true;
  }

  closeResetPasswordModal(): void {
    this.showResetPasswordModal = false;
    this.selectedUserForReset = null;
  }

  submitPasswordReset(): void {
    if (!this.selectedUserForReset || !this.newPasswordToSet || this.newPasswordToSet.length < 6) {
      alert('Password must be at least 6 characters long.');
      return;
    }

    this.adminApi.resetUserPassword(this.selectedUserForReset.userId, this.newPasswordToSet).subscribe({
      next: () => {
        alert(`Password for ${this.selectedUserForReset.fullName} has been reset successfully.`);
        this.closeResetPasswordModal();
      },
      error: (err) => alert(err.error?.error || 'Failed to reset password')
    });
  }

  deleteUser(user: any): void {
    if (confirm(`Are you sure you want to permanently delete user "${user.fullName}"?`)) {
      this.adminApi.deleteUser(user.userId).subscribe({
        next: () => this.loadUsers(),
        error: (err) => alert(err.error?.error || 'Failed to delete user')
      });
    }
  }
}
