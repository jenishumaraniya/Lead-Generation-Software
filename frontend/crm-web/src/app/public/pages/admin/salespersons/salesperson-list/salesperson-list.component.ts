import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { EmployeeService, Salesperson } from '../../../../../core/services/employee.service';
import { CategoryService, Category } from '../../../../../core/services/category.service';

@Component({
  selector: 'app-salesperson-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './salesperson-list.component.html',
  styleUrls: ['./salesperson-list.component.css']
})
export class SalespersonListComponent implements OnInit {
  salespersons: Salesperson[] = [];
  categories: Category[] = [];
  loading = false;
  errorMessage = '';
  successMessage = '';

  // Add Salesperson Modal State
  showAddModal = false;
  newRep = {
    fullName: '',
    email: '',
    password: '',
    categoryId: null as number | null
  };

  // Change Password Modal State
  showPasswordModal = false;
  selectedRep: Salesperson | null = null;
  newPassword = '';

  constructor(
    private employeeService: EmployeeService,
    private categoryService: CategoryService
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loading = true;
    this.categoryService.getCategories().subscribe({
      next: (cats) => {
        this.categories = cats || [];
        this.employeeService.getEmployees().subscribe({
          next: (users) => {
            this.salespersons = (users || []).filter(u => u.role === 'SALES_REP');
            this.loading = false;
          },
          error: () => {
            this.salespersons = [];
            this.loading = false;
          }
        });
      },
      error: () => {
        this.categories = [];
        this.loading = false;
      }
    });
  }

  isCategoryAssignedToOther(categoryId: number, currentUserId?: number): boolean {
    return this.salespersons.some(
      s => s.categoryId === categoryId && s.userId !== currentUserId && s.isActive
    );
  }

  getAssignedRepNameForCategory(categoryId: number): string {
    const rep = this.salespersons.find(s => s.categoryId === categoryId && s.isActive);
    return rep ? rep.fullName : '';
  }

  openAddModal(): void {
    this.newRep = { fullName: '', email: '', password: '', categoryId: null };
    this.errorMessage = '';
    this.showAddModal = true;
  }

  closeAddModal(): void {
    this.showAddModal = false;
  }

  createSalesperson(): void {
    if (!this.newRep.fullName || !this.newRep.email || !this.newRep.password) {
      this.errorMessage = 'Please complete all required fields.';
      return;
    }

    if (this.newRep.categoryId && this.isCategoryAssignedToOther(this.newRep.categoryId)) {
      const repName = this.getAssignedRepNameForCategory(this.newRep.categoryId);
      this.errorMessage = `Selected category is already assigned to '${repName}'. No two sales persons can be in the same category.`;
      return;
    }

    this.employeeService.createSalesperson(this.newRep).subscribe({
      next: () => {
        this.closeAddModal();
        this.showSuccess('Sales representative added successfully with assigned category.');
        this.loadData();
      },
      error: (err) => {
        this.errorMessage = err.error?.error || 'Failed to add sales representative.';
      }
    });
  }

  onCategorySelected(rep: Salesperson, newCatId: number | null): void {
    if (newCatId && this.isCategoryAssignedToOther(newCatId, rep.userId)) {
      const repName = this.getAssignedRepNameForCategory(newCatId);
      alert(`Category is already assigned to '${repName}'. No two sales persons can be in the same category.`);
      this.loadData();
      return;
    }

    this.employeeService.assignCategory(rep.userId, newCatId).subscribe({
      next: (res) => {
        rep.categoryId = res.categoryId;
        rep.categoryName = res.categoryName;
        this.showSuccess(`Category '${res.categoryName || 'None'}' assigned to ${rep.fullName}.`);
        this.loadData();
      },
      error: (err) => {
        alert(err.error?.error || 'Failed to update category.');
        this.loadData();
      }
    });
  }

  openPasswordModal(rep: any): void {
    this.selectedRep = rep;
    this.newPassword = '';
    this.errorMessage = '';
    this.showPasswordModal = true;
  }

  closePasswordModal(): void {
    this.showPasswordModal = false;
    this.selectedRep = null;
  }

  resetPassword(): void {
    if (!this.selectedRep || !this.newPassword.trim()) {
      this.errorMessage = 'Please enter a new password.';
      return;
    }

    this.employeeService.resetPassword(this.selectedRep.userId, this.newPassword.trim()).subscribe({
      next: () => {
        this.closePasswordModal();
        this.showSuccess(`Password updated successfully for ${this.selectedRep?.fullName}.`);
      },
      error: (err) => {
        this.errorMessage = err.error?.error || 'Failed to reset password.';
      }
    });
  }

  deleteSalesperson(rep: any): void {
    if (!confirm(`Are you sure you want to remove ${rep.fullName}? This will unassign all their leads.`)) {
      return;
    }

    this.employeeService.deleteSalesperson(rep.userId).subscribe({
      next: () => {
        this.showSuccess(`Sales representative ${rep.fullName} removed.`);
        this.loadData();
      },
      error: (err) => {
        alert(err.error?.error || 'Failed to delete sales representative.');
      }
    });
  }

  showSuccess(msg: string): void {
    this.successMessage = msg;
    setTimeout(() => this.successMessage = '', 3500);
  }
}