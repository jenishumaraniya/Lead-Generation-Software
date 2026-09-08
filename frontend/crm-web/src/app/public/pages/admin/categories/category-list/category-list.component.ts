import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { Category } from '../../../../../core/models/category.model';
import { CategoryService } from '../../../../../core/services/category.service';
import { EmployeeService } from '../../../../../core/services/employee.service';
import { PaginationComponent } from '../../../../../components/pagination/pagination.component';

@Component({
  selector: 'app-category-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, PaginationComponent],
  templateUrl: './category-list.component.html',
  styleUrls: ['./category-list.component.css']
})
export class CategoryListComponent implements OnInit {
  categories: Category[] = [];
  filteredCategories: Category[] = [];
  salespersons: any[] = [];
  salespersonsCount = 0;
  
  searchTerm: string = '';
  selectedFilter: string = 'ALL';
  viewMode: 'cards' | 'table' = 'cards';

  sortColumn: string = 'categoryName';
  sortDirection: 'asc' | 'desc' = 'asc';

  currentPage = 1;
  pageSize = 10;

  get paginatedCategories(): Category[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filteredCategories.slice(start, start + this.pageSize);
  }

  stats = {
    totalCategories: 0,
    coveredCount: 0,
    unassignedCount: 0,
    totalReps: 0
  };

  showModal = false;
  isEdit = false;
  editingId: number | null = null;
  categoryName = '';
  loading = false;
  noticeMessage = '';

  constructor(
    private categoryService: CategoryService,
    private employeeService: EmployeeService
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loading = true;
    this.categoryService.getCategories().subscribe({
      next: (data) => {
        this.categories = data || [];
        this.employeeService.getEmployees().subscribe({
          next: (users) => {
            this.salespersons = (users || []).filter(u => u.role === 'SALES_REP' && u.isActive);
            this.salespersonsCount = this.salespersons.length;
            this.calculateStats();
            this.applyFilter();
            this.loading = false;
          },
          error: () => {
            this.salespersons = [];
            this.salespersonsCount = 0;
            this.calculateStats();
            this.applyFilter();
            this.loading = false;
          }
        });
      },
      error: () => {
        this.categories = [];
        this.filteredCategories = [];
        this.calculateStats();
        this.loading = false;
      }
    });
  }

  calculateStats(): void {
    this.stats.totalCategories = this.categories.length;
    this.stats.totalReps = this.salespersonsCount;
    
    const assignedCategoryIds = new Set(this.salespersons.filter(s => s.categoryId).map(s => s.categoryId));
    this.stats.coveredCount = this.categories.filter(c => assignedCategoryIds.has(c.categoryId)).length;
    this.stats.unassignedCount = this.categories.filter(c => !assignedCategoryIds.has(c.categoryId)).length;
  }

  getAssignedSalesperson(categoryId: number): any {
    return this.salespersons.find(s => s.categoryId === categoryId);
  }

  setViewMode(mode: 'cards' | 'table'): void {
    this.viewMode = mode;
    this.currentPage = 1;
  }

  applyFilter(): void {
    this.currentPage = 1;
    const term = this.searchTerm.trim().toLowerCase();

    let list = this.categories.filter(cat => {
      const assignedRep = this.getAssignedSalesperson(cat.categoryId);

      if (this.selectedFilter === 'ASSIGNED' && !assignedRep) return false;
      if (this.selectedFilter === 'UNASSIGNED' && assignedRep) return false;

      if (!term) return true;

      const nameMatch = cat.categoryName?.toLowerCase().includes(term);
      const repMatch = assignedRep?.fullName?.toLowerCase().includes(term);
      return nameMatch || repMatch;
    });

    if (this.sortColumn) {
      list.sort((a: any, b: any) => {
        let valA = a[this.sortColumn];
        let valB = b[this.sortColumn];

        if (this.sortColumn === 'rep') {
          const repA = this.getAssignedSalesperson(a.categoryId);
          const repB = this.getAssignedSalesperson(b.categoryId);
          valA = repA ? repA.fullName : '';
          valB = repB ? repB.fullName : '';
        }

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

    this.filteredCategories = list;
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

  openAddModal(): void {
    this.isEdit = false;
    this.editingId = null;
    this.categoryName = '';
    this.showModal = true;
  }

  openEditModal(category: Category): void {
    this.isEdit = true;
    this.editingId = category.categoryId;
    this.categoryName = category.categoryName;
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
    this.categoryName = '';
  }

  saveCategory(): void {
    if (!this.categoryName.trim()) return;
    this.loading = true;
    const data = { categoryName: this.categoryName.trim() };
    const obs = this.isEdit
      ? this.categoryService.updateCategory(this.editingId!, data)
      : this.categoryService.createCategory(data);

    obs.subscribe({
      next: () => {
        this.loadData();
        this.closeModal();
        this.loading = false;
        if (!this.isEdit) {
          this.noticeMessage = 'Category created successfully. Reminder: ensure a Sales Representative is available to handle leads in this category.';
          setTimeout(() => this.noticeMessage = '', 6000);
        }
      },
      error: () => {
        alert('Failed to save category');
        this.loading = false;
      }
    });
  }

  deleteCategory(cat: any): void {
    if (cat.productsCount && cat.productsCount > 0) {
      alert(`Cannot delete category '${cat.categoryName}' because it contains ${cat.productsCount} associated product(s). Please delete or reassign the products first.`);
      return;
    }

    if (confirm(`Are you sure you want to delete the category '${cat.categoryName}'?`)) {
      this.loading = true;
      this.categoryService.deleteCategory(cat.categoryId).subscribe({
        next: () => {
          this.loadData();
          this.loading = false;
        },
        error: (err) => {
          alert(err.error?.error || 'Failed to delete category');
          this.loading = false;
        }
      });
    }
  }
}