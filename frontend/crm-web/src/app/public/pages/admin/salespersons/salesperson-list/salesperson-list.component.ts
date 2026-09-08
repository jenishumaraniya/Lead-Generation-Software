import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { EmployeeService, Salesperson } from '../../../../../core/services/employee.service';
import { CategoryService, Category } from '../../../../../core/services/category.service';
import { PaginationComponent } from '../../../../../components/pagination/pagination.component';

@Component({
  selector: 'app-salesperson-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, PaginationComponent],
  templateUrl: './salesperson-list.component.html',
  styleUrls: ['./salesperson-list.component.css']
})
export class SalespersonListComponent implements OnInit {
  salespersons: Salesperson[] = [];
  filteredSalespersons: Salesperson[] = [];
  categories: Category[] = [];
  loading = false;
  errorMessage = '';
  successMessage = '';

  searchTerm: string = '';
  selectedFilter: string = 'ALL';
  viewMode: 'cards' | 'table' = 'cards';

  sortColumn: string = 'fullName';
  sortDirection: 'asc' | 'desc' = 'asc';

  currentPage = 1;
  pageSize = 10;

  get paginatedSalespersons(): Salesperson[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filteredSalespersons.slice(start, start + this.pageSize);
  }

  stats = {
    totalReps: 0,
    activeReps: 0,
    assignedCount: 0,
    totalCategories: 0,
    unassignedCategoriesCount: 0
  };

  // Add Salesperson Modal State
  showAddModal = false;
  showInitialPassword = false;
  newRep = {
    fullName: '',
    email: '',
    password: '',
    categoryId: null as number | null
  };

  // Change Password Modal State
  showPasswordModal = false;
  showNewPassword = false;
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
            this.calculateStats();
            this.applyFilter();
            this.loading = false;
          },
          error: () => {
            this.salespersons = [];
            this.filteredSalespersons = [];
            this.calculateStats();
            this.loading = false;
          }
        });
      },
      error: () => {
        this.categories = [];
        this.salespersons = [];
        this.filteredSalespersons = [];
        this.calculateStats();
        this.loading = false;
      }
    });
  }

  calculateStats(): void {
    this.stats.totalReps = this.salespersons.length;
    this.stats.activeReps = this.salespersons.filter(s => s.isActive).length;
    this.stats.assignedCount = this.salespersons.filter(s => s.categoryId && s.isActive).length;
    this.stats.totalCategories = this.categories.length;
    const assignedCategoryIds = new Set(this.salespersons.filter(s => s.categoryId && s.isActive).map(s => s.categoryId));
    this.stats.unassignedCategoriesCount = this.categories.filter(c => !assignedCategoryIds.has(c.categoryId)).length;
  }

  setViewMode(mode: 'cards' | 'table'): void {
    this.viewMode = mode;
    this.currentPage = 1;
  }

  applyFilter(): void {
    this.currentPage = 1;
    const term = this.searchTerm.trim().toLowerCase();

    let list = this.salespersons.filter(rep => {
      // Filter logic
      if (this.selectedFilter === 'ASSIGNED' && !rep.categoryId) return false;
      if (this.selectedFilter === 'UNASSIGNED' && rep.categoryId) return false;
      if (this.selectedFilter === 'ACTIVE' && !rep.isActive) return false;

      // Search term
      if (!term) return true;

      const nameMatch = rep.fullName?.toLowerCase().includes(term);
      const emailMatch = rep.email?.toLowerCase().includes(term);
      const catMatch = (rep.categoryName || '').toLowerCase().includes(term);

      return nameMatch || emailMatch || catMatch;
    });

    if (this.sortColumn) {
      list.sort((a: any, b: any) => {
        let valA = a[this.sortColumn];
        let valB = b[this.sortColumn];

        if (valA == null) valA = '';
        if (valB == null) valB = '';

        if (typeof valA === 'string') valA = valA.toLowerCase();
        if (typeof valB === 'string') valB = valB.toLowerCase();

        let comparison = 0;
        if (valA > valB) comparison = 1;
        else if (valA < valB) comparison = -1;

        return this.sortDirection === 'asc' ? comparison : -comparison;
      });
    }

    this.filteredSalespersons = list;
  }

  toggleSort(column: string): void {
    if (this.sortColumn === column) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortColumn = column;
      this.sortDirection = 'asc';
    }
    this.applyFilter();
  }

  getInitials(name?: string): string {
    if (!name) return 'SR';
    const parts = name.trim().split(' ');
    if (parts.length >= 2) {
      return (parts[0][0] + parts[1][0]).toUpperCase();
    }
    return name.slice(0, 2).toUpperCase();
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